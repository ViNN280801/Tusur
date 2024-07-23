using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TusurUI.Source;
using TusurUI.Helpers;

namespace TusurUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly PowerSupplyTimerManager _powerSupplyTimerManager;
        private readonly ComPortManager _powerSupplyComPortManager;
        private readonly ComPortManager _stepMotorComPortManager;
        private readonly PowerSupplyManager _powerSupplyManager;
        private readonly StepMotorManager _stepMotorManager;
        private readonly UIHelper _uiHelper;

        private DispatcherTimer? _comPortUpdateTimer;
        private DispatcherTimer? _currentVoltageUpdateTimer;

        private const int UpdateComPortsIntervalMilliseconds = 5000;
        private const int UpdateCurrentVoltageIntervalMilliseconds = 100;
        private bool isVaporizerWorks = false;
        private double currentValue { get; set; }
        private ushort voltageValue = 6;

        public MainWindow()
        {
            InitializeComponent();

            _uiHelper = new UIHelper(Vaporizer, SystemStateLabel, VaporizerButtonBase, VaporizerButtonInside, Indicator, CurrentValueLabel, VoltageValueLabel);
            _powerSupplyTimerManager = new PowerSupplyTimerManager(TimerTextBox, TurnOffPowerSupply);
            _powerSupplyComPortManager = new ComPortManager(PowerSupplyComPortComboBox);
            _stepMotorComPortManager = new ComPortManager(ShutterComPortComboBox);
            _powerSupplyManager = new PowerSupplyManager(CurrentValueLabel, VoltageValueLabel);
            _stepMotorManager = new StepMotorManager(_uiHelper);

            InitializeComPortUpdateTimer();
            InitializeUpdateCurrentVoltageTimer();

            // If shutter opened when program started - change icon.
            if (_stepMotorManager.IsShutterOpened())
                _uiHelper.SetShutterImageToOpened();
            else
                _uiHelper.SetShutterImageToClosed();
        }

        private void InitializeComPortUpdateTimer()
        {
            _comPortUpdateTimer = new DispatcherTimer();
            _comPortUpdateTimer.Interval = TimeSpan.FromMilliseconds(UpdateComPortsIntervalMilliseconds);
            _comPortUpdateTimer.Tick += ComPortUpdateTimer_Tick;
            _comPortUpdateTimer.Start();
        }

        private void ComPortUpdateTimer_Tick(object? sender, EventArgs e)
        {
            _powerSupplyComPortManager.PopulateComPortComboBox();
            _stepMotorComPortManager.PopulateComPortComboBox();
        }

        private void InitializeUpdateCurrentVoltageTimer()
        {
            _currentVoltageUpdateTimer = new DispatcherTimer();
            _currentVoltageUpdateTimer.Interval = TimeSpan.FromMilliseconds(UpdateCurrentVoltageIntervalMilliseconds);
            _currentVoltageUpdateTimer.Tick += CurrentVoltageUpdateTimer_Tick;
        }

        private void CurrentVoltageUpdateTimer_Tick(object? sender, EventArgs e)
        {
            _powerSupplyManager.ReadCurrentVoltageAndChangeTextBox();
        }

        private void StopTimers()
        {
            _comPortUpdateTimer?.Stop();
            _currentVoltageUpdateTimer?.Stop();
        }

        private void TimerTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (int.TryParse(textBox.Text, out int minutes) && minutes > 0)
                {
                    textBox.ClearValue(Border.BorderBrushProperty);
                    textBox.ClearValue(Border.BorderThicknessProperty);
                    StartButton.IsEnabled = true;
                    textBox.ToolTip = "Введите значение в минутах";
                }
                else
                {
                    textBox.BorderBrush = new SolidColorBrush(Colors.Red);
                    textBox.BorderThickness = new Thickness(1);
                    textBox.ToolTip = "Неверное значение. Допустимый диапазон: > 0 минут";
                }
            }
        }

        private void TimerTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !_powerSupplyTimerManager.IsValidTimerInput(e.Text);
        }

        private void StartCountdown()
        {
            try
            {
                _powerSupplyTimerManager.StartCountdown();
            }
            catch (ArgumentException ex)
            {
                ShowError(ex.Message);
            }
        }

        private bool AreComPortsValid()
        {
            try
            {
                _powerSupplyComPortManager.CheckComPort();
            }
            catch (ArgumentException)
            {
                ShowError("Сначала выберите COM-порт для блока питания.");
                return false;
            }

            try
            {
                _stepMotorComPortManager.CheckComPort();
            }
            catch (ArgumentException)
            {
                ShowError("Сначала выберите COM-порт для шагового двигателя.");
                return false;
            }

            return true;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            StopTimers();
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
            ExecuteWithErrorHandling(() =>
            {
                if (!_powerSupplyComPortManager.CheckComPort())
                    return;

                string comPort = _powerSupplyComPortManager.GetComPortName();
                _powerSupplyManager.ConnectToPowerSupply(comPort);
                _currentVoltageUpdateTimer?.Start();
            }, UncheckVaporizerButton);
        }

        private void TurnOnPowerSupply()
        {
            ExecuteWithErrorHandling(() =>
            {
                string comPort = _powerSupplyComPortManager.GetComPortName();
                _powerSupplyManager.TurnOnPowerSupply(comPort);
                _currentVoltageUpdateTimer?.Start();
            }, UncheckVaporizerButton);
        }

        private void Reset()
        {
            ExecuteWithErrorHandling(() =>
            {
                string comPort = _powerSupplyComPortManager.GetComPortName();
                _powerSupplyManager.Reset(comPort);
            }, UncheckVaporizerButton);
        }

        private void ApplyVoltageOnPowerSupply()
        {
            ExecuteWithErrorHandling(() =>
            {
                _powerSupplyManager.ApplyVoltageOnPowerSupply(currentValue, voltageValue);
            }, () =>
            {
                TurnOffPowerSupply();
                UncheckVaporizerButton();
            });
        }

        private void ReadCurrentVoltageAndChangeTextBox()
        {
            ExecuteWithErrorHandling(() =>
            {
                _powerSupplyManager.ReadCurrentVoltageAndChangeTextBox();
            });
        }

        private void TurnOffPowerSupply()
        {
            ExecuteWithErrorHandling(() =>
            {
                string comPort = _powerSupplyComPortManager.GetComPortName();
                _powerSupplyManager.TurnOffPowerSupply(comPort);
                _currentVoltageUpdateTimer?.Stop();
            }, UncheckVaporizerButton);
        }

        private void OpenShutter()
        {
            ExecuteWithErrorHandling(() =>
            {
                string comPort = _stepMotorComPortManager.GetComPortName();
                _stepMotorManager.OpenShutter(comPort);
                SetShutterImageToOpened();
                ColorizeOpenShutterButton();
            }, () =>
            {
                SetShutterImageToClosed();
                ColorizeCloseShutterButton();
            });
        }

        private void CloseShutter()
        {
            ExecuteWithErrorHandling(() =>
            {
                string comPort = _stepMotorComPortManager.GetComPortName();
                _stepMotorManager.CloseShutter(comPort);
                SetShutterImageToClosed();
                ColorizeCloseShutterButton();
            }, SetShutterImageToClosed);
        }

        private void StopStepMotor()
        {
            ExecuteWithErrorHandling(() =>
            {
                string comPort = _stepMotorComPortManager.GetComPortName();
                _stepMotorManager.StopStepMotor(comPort);
                ColorizeStopStepMotorButton();
            });
        }

        private void ShowWarning(string message, string title = "Предупреждение") { MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning); }

        private void ShowError(string message, string title = "Ошибка") { MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error); }

        private void CheckVaporizerButton()
        {
            _uiHelper.CheckVaporizerButton();
            isVaporizerWorks = true;
        }

        private void UncheckVaporizerButton()
        {
            _uiHelper.UncheckVaporizerButton();
            isVaporizerWorks = false;
            _powerSupplyTimerManager.ResetTimer();
            _currentVoltageUpdateTimer?.Stop();
        }

        private void SetShutterImageToClosed() { _uiHelper.SetShutterImageToClosed(); ; }

        private void SetShutterImageToOpened() { _uiHelper.SetShutterImageToOpened(); }

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
            if (!AreComPortsValid())
                return;

            if (isVaporizerWorks)
            {
                SystemStateLabel.Content = "Система работает";
                SystemStateLabel.Foreground = new SolidColorBrush(Colors.Green);

                try
                {
                    TurnOnPowerSupply();
                    ApplyVoltageOnPowerSupply();
                    ReadCurrentVoltageAndChangeTextBox();
                    Reset();

                    StartCountdown();
                    StartButton.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
            }
            else
                ShowWarning("Сперва нужно включить управление испарителем");
        }

        private void ColorizeOpenShutterButton() { _uiHelper.ColorizeOpenShutterButton(OpenShutterButton, CloseShutterButton, StopStepMotorButton); }
        private void ColorizeCloseShutterButton() { _uiHelper.ColorizeCloseShutterButton(OpenShutterButton, CloseShutterButton, StopStepMotorButton); }
        private void ColorizeStopStepMotorButton() { _uiHelper.ColorizeStopStepMotorButton(OpenShutterButton, CloseShutterButton, StopStepMotorButton); }

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

        private void SetEnableOpenCloseShutterButtons()
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
            SetEnableOpenCloseShutterButtons();
            StopStepMotor();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            TurnOffPowerSupply();
            StopStepMotor();
        }

        private void ExecuteWithErrorHandling(Action action, Action? onError = null)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
                onError?.Invoke();
            }
        }
    }
}
