using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.IO.Ports;
using TusurUI.Source;
using TusurUI.ExternalSources;
using System.Windows.Threading;
using System.Diagnostics.SymbolStore;

namespace TusurUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer? _comPortUpdateTimer;
        private DispatcherTimer? _statusCheckTimer;

        private bool isVaporizerWorks = false;
        private double currentValue { get; set; }
        private ushort voltageValue = 6;

        private string powerSupplyCOM = "";
        private string stepMotorCOM = "";

        public MainWindow()
        {
            InitializeComponent();
            InitializeComPortUpdateTimer();
            // InitializeStatusCheckTimer();
            PopulateComPortComboBoxes();

            // If shutter opened when program started - change icon.
            if (IsShutterOpened())
                SetShutterImageToOpened();
            else
                SetShutterImageToClosed();
        }

        private void InitializeComPortUpdateTimer()
        {
            _comPortUpdateTimer = new DispatcherTimer();
            _comPortUpdateTimer.Interval = TimeSpan.FromSeconds(5);
            _comPortUpdateTimer.Tick += ComPortUpdateTimer_Tick;
            _comPortUpdateTimer.Start();
        }

        private void ComPortUpdateTimer_Tick(object? sender, EventArgs e) { PopulateComPortComboBoxes(); }

        private void InitializeStatusCheckTimer()
        {
            _statusCheckTimer = new DispatcherTimer();
            _statusCheckTimer.Interval = TimeSpan.FromMilliseconds(100);
            _statusCheckTimer.Tick += StatusCheckTimer_Tick;
            _statusCheckTimer.Start();
        }

        private void StatusCheckTimer_Tick(object? sender, EventArgs e) { CheckMotorStatus(); }

        enum MotorState { Idle, Forward, Reverse }
        MotorState lastMotorState = MotorState.Idle;
        private void CheckMotorStatus()
        {
            bool isForwardPressed = StepMotor.IsForwardButtonPressed();
            bool isReversePressed = StepMotor.IsReverseButtonPressed();

            if (isForwardPressed && lastMotorState != MotorState.Reverse)
            {
                // Заслонка движется вперед, активировался датчик FWD
                StepMotor.Stop();
                lastMotorState = MotorState.Forward;
            }
            else if (isReversePressed && lastMotorState != MotorState.Forward)
            {
                // Заслонка движется назад, активировался датчик REV
                StepMotor.Stop();
                lastMotorState = MotorState.Reverse;
            }
            else if (!isForwardPressed && !isReversePressed)
            {
                // Ни один из датчиков не активен
                if (lastMotorState == MotorState.Forward)
                {
                    // Последнее направление было вперед, двигатель остановлен, но датчик FWD не активен
                    // Предполагается, что заслонка не достигла конечного положения
                    StepMotor.Forward();
                }
                else if (lastMotorState == MotorState.Reverse)
                {
                    // Последнее направление было назад, двигатель остановлен, но датчик REV не активен
                    // Предполагается, что заслонка не достигла конечного положения
                    StepMotor.Reverse();
                }
            }
            else
            {
                // Ситуация, когда оба датчика активированы одновременно, является исключением
                StepMotor.Stop();
                lastMotorState = MotorState.Idle;
            }
        }

        private void PopulateComPortComboBoxes()
        {
            string[] ports = SerialPort.GetPortNames();
            PowerSupplyComPortComboBox.ItemsSource = ports;
            ShutterComPortComboBox.ItemsSource = ports;

            if (ports.Length > 0)
            {
                if (!PowerSupplyComPortComboBox.Items.Contains(PowerSupplyComPortComboBox.SelectedItem))
                    PowerSupplyComPortComboBox.SelectedIndex = 0;
                if (!ShutterComPortComboBox.Items.Contains(ShutterComPortComboBox.SelectedItem))
                    ShutterComPortComboBox.SelectedIndex = 0;
                PowerSupplyComPortComboBox.IsEnabled = true;
                ShutterComPortComboBox.IsEnabled = true;
            }
            else
            {
                PowerSupplyComPortComboBox.IsEnabled = false;
                ShutterComPortComboBox.IsEnabled = false;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _comPortUpdateTimer?.Stop();
        }

        private bool IsValidInput(string text)
        {
            foreach (var c in text)
                if (!char.IsDigit(c) && c != ',' && c != '.')
                    return false;
            return true;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender == PowerSupplyComPortComboBox && PowerSupplyComPortComboBox.SelectedItem == ShutterComPortComboBox.SelectedItem)
                ShutterComPortComboBox.SelectedIndex = -1;
            else if (sender == ShutterComPortComboBox && ShutterComPortComboBox.SelectedItem == PowerSupplyComPortComboBox.SelectedItem)
                PowerSupplyComPortComboBox.SelectedIndex = -1;
        }

        private void ConnectToPowerSupply()
        {
            try
            {
                if (IsPowerSupplyCOMportNull())
                    return;
                PowerSupply.Connect(powerSupplyCOM);
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                UncheckVaporizerButton();
            }
        }

        private void TurnOnPowerSupply()
        {
            try
            {
                if (IsPowerSupplyCOMportNull())
                    return;
                PowerSupply.Connect(powerSupplyCOM);

                if (IsPowerSupplyErrorCodeStatusFailed(PowerSupply.TurnOn()))
                    return;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                UncheckVaporizerButton();
            }
        }

        private void ResetZP()
        {
            try
            {
                if (IsPowerSupplyCOMportNull())
                    return;
                PowerSupply.Connect(powerSupplyCOM);

                if (IsPowerSupplyErrorCodeStatusFailed(PowerSupply.ResetZP()))
                    return;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                UncheckVaporizerButton();
            }
        }

        private void ApplyVoltageOnPowerSupply()
        {
            try
            {
                if (IsPowerSupplyErrorCodeStatusFailed(PowerSupply.SetCurrentVoltage((ushort)currentValue, voltageValue)))
                    return;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                TurnOffPowerSupply();
                UncheckVaporizerButton();
            }
        }

        private void ReadCurrentVoltageAndChangeTextBox()
        {
            try
            {
                int current = PowerSupply.ReadCurrent();
                if (current == -1)
                {
                    ShowWarning(PowerSupply.GetErrorMessage(current));
                    return;
                }

                CurrentValueLabel.Content = current.ToString() + " A";

                int voltage = PowerSupply.ReadVoltage();
                if (voltage == -2)
                {
                    ShowWarning(PowerSupply.GetErrorMessage(voltage));
                    return;
                }

                VoltageValueLabel.Content = voltage.ToString() + " В";
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void TurnOffPowerSupply()
        {
            try
            {
                if (IsPowerSupplyErrorCodeStatusFailed(PowerSupply.TurnOff()))
                    return;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                UncheckVaporizerButton();
            }
        }

        private void OpenShutter()
        {
            try
            {
                if (IsStepMotorCOMportNull())
                    return;
                StepMotor.Connect(stepMotorCOM);

                if (IsStepMotorErrorCodeStatusFailed(StepMotor.Forward()))
                    return;

                lastMotorState = MotorState.Forward;
                SetShutterImageToOpened();
                ColorizeOpenShutterButton();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                SetShutterImageToClosed();
            }
        }

        private void CloseShutter()
        {
            try
            {
                if (IsStepMotorCOMportNull())
                    return;
                StepMotor.Connect(stepMotorCOM);

                if (IsStepMotorErrorCodeStatusFailed(StepMotor.Reverse()))
                    return;

                lastMotorState = MotorState.Reverse;
                SetShutterImageToClosed();
                ColorizeCloseShutterButton();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                SetShutterImageToClosed();
            }
        }

        private void StopStepMotor()
        {
            try
            {
                if (IsStepMotorCOMportNull())
                    return;
                StepMotor.Connect(stepMotorCOM);

                if (IsStepMotorErrorCodeStatusFailed(StepMotor.Stop()))
                    return;

                lastMotorState = MotorState.Idle;
                ColorizeStopStepMotorButton();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void ShowWarning(string message, string title = "Предупреждение") { MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning); }

        private void ShowError(string message, string title = "Ошибка") { MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error); }

        private bool IsShutterOpened() { return StepMotor.IsForwardButtonPressed(); }
        private bool IsShutterClosed() { return StepMotor.IsReverseButtonPressed(); }

        private bool IsPowerSupplyErrorCodeStatusFailed(int errorCode)
        {
            if (errorCode > 0)
            {
                ShowWarning(PowerSupply.GetErrorMessage(errorCode));
                UncheckVaporizerButton();
            }
            return errorCode == 0;
        }

        private bool IsStepMotorErrorCodeStatusFailed(int errorCode)
        {
            if (errorCode > 0)
            {
                ShowWarning(StepMotor.GetErrorMessage(errorCode));
                SetShutterImageToClosed();
            }
            return errorCode == 0;
        }

        private bool IsPowerSupplyCOMportNull()
        {
            string? port = PowerSupplyComPortComboBox.SelectedItem.ToString();
            if (port == null)
            {
                ShowError($"Возникла ошибка при выборе COM-порта для управления блоком питания. Выбранный COM-порт: <{PowerSupplyComPortComboBox.SelectedItem}>");
                UncheckVaporizerButton();
                return true;
            }
            powerSupplyCOM = port;
            return false;
        }

        private bool IsStepMotorCOMportNull()
        {
            string? port = ShutterComPortComboBox.SelectedItem.ToString();
            if (port == null)
            {
                ShowError($"Возникла ошибка при выборе COM-порта для управления заслонкой. Выбранный COM-порт: <{ShutterComPortComboBox.SelectedItem}>");
                SetShutterImageToClosed();
                return true;
            }
            stepMotorCOM = port;
            return false;
        }

        private void CheckVaporizerButton()
        {
            // Changing color of the switch and moving button to the right
            VaporizerButtonBase.Background = new SolidColorBrush(Colors.Green);
            DockPanel.SetDock(VaporizerButtonInside, Dock.Right);
            ComponentManager.ChangeIndicatorPicture(Indicator, "Images/индикатор вкл.jpg");
            isVaporizerWorks = true;
        }

        private void UncheckVaporizerButton()
        {
            SystemStateLabel.Content = "Система не работает";
            SystemStateLabel.Foreground = new SolidColorBrush(Colors.Red);
            VaporizerButtonBase.Background = new SolidColorBrush(Colors.White);
            DockPanel.SetDock(VaporizerButtonInside, Dock.Left);
            ComponentManager.ChangeIndicatorPicture(Indicator, "Images/индикатор откл.jpg");
            isVaporizerWorks = false;

            CurrentValueLabel.Content = "0 A";
            VoltageValueLabel.Content = "0 В";
        }

        private void SetShutterImageToClosed() { ComponentManager.ChangeIndicatorPicture(Vaporizer, "Images/заслонка закр фото.png"); }

        private void SetShutterImageToOpened() { ComponentManager.ChangeIndicatorPicture(Vaporizer, "Images/заслонка откр.png"); }

        private void VaporizerButtonBase_Checked(object sender, RoutedEventArgs e)
        {
            CheckVaporizerButton();
            ConnectToPowerSupply();
        }

        private void VaporizerButtonBase_Unchecked(object sender, RoutedEventArgs e)
        {
            UncheckVaporizerButton();
            TurnOffPowerSupply();
        }

        private void CurrentSetPointTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string newText = textBox.Text.Replace(',', '.');
                if (double.TryParse(newText,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double value) && (value >= 0 && value <= 200))
                {
                    textBox.ClearValue(Border.BorderBrushProperty);
                    textBox.ClearValue(Border.BorderThicknessProperty);
                    StartButton.IsEnabled = true;
                    currentValue = value;
                    textBox.ToolTip = "Введите значение от 0 до 200";

                    if (value >= 160)
                        ShowWarning("Опасность перегрева тигля. Введенное значение тока близко к максимуму. Максимальное значение - 200 А");
                }
                else
                {
                    textBox.BorderBrush = new SolidColorBrush(Colors.Red);
                    textBox.BorderThickness = new Thickness(1);
                    StartButton.IsEnabled = false;
                    textBox.ToolTip = "Неверное значение. Допустимый диапазон: 0-200 А";
                }
            }
        }
        private void CurrentSetPointTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) { e.Handled = !IsValidInput(e.Text); }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (isVaporizerWorks)
            {
                SystemStateLabel.Content = "Система работает";
                SystemStateLabel.Foreground = new SolidColorBrush(Colors.Green);

                try
                {
                    TurnOnPowerSupply();
                    ApplyVoltageOnPowerSupply();
                    ReadCurrentVoltageAndChangeTextBox();
                    ResetZP();
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
            }
            else
                ShowWarning("Сперва нужно включить управление испарителем");
        }

        private void ColorizeOpenShutterButton()
        {
            StopStepMotorButton.Background = new SolidColorBrush(Colors.White);
            CloseShutterButton.Background = new SolidColorBrush(Colors.White);
            OpenShutterButton.Background = new SolidColorBrush(Colors.Green);
        }

        private void ColorizeCloseShutterButton()
        {
            StopStepMotorButton.Background = new SolidColorBrush(Colors.White);
            CloseShutterButton.Background = new SolidColorBrush(Colors.Red);
            OpenShutterButton.Background = new SolidColorBrush(Colors.White);
        }

        private void ColorizeStopStepMotorButton()
        {
            StopStepMotorButton.Background = new SolidColorBrush(Colors.Gray);
            CloseShutterButton.Background = new SolidColorBrush(Colors.White);
            OpenShutterButton.Background = new SolidColorBrush(Colors.White);
        }

        private void SetDisabledOpenShutterButton()
        {
            OpenShutterButton.IsEnabled = false;
            CloseShutterButton.IsEnabled = true;
        }

        private void SetDisabledCloseShutterButton()
        {
            OpenShutterButton.IsEnabled = true;
            CloseShutterButton.IsEnabled = false;
        }

        private void SetEnablesOpenCloseShutterButtons()
        {
            OpenShutterButton.IsEnabled = true;
            CloseShutterButton.IsEnabled = true;
        }

        private void OpenShutterButton_Click(object sender, RoutedEventArgs e)
        {
            SetDisabledOpenShutterButton();
            OpenShutter();
        }

        private void CloseShutterButton_Click(object sender, RoutedEventArgs e)
        {
            SetDisabledCloseShutterButton();
            CloseShutter();
        }

        private void StopStepMotorButton_Click(object sender, RoutedEventArgs e)
        {
            SetEnablesOpenCloseShutterButtons();
            StopStepMotor();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            TurnOffPowerSupply();
            StopStepMotor();
        }
    }
}
