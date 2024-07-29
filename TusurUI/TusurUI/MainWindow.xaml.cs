using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using TusurUI.Source;
using TusurUI.Helpers;
using TusurUI.Interfaces;

namespace TusurUI
{
    public partial class MainWindow : Window
    {
        private const int k_InvalidComboBoxItem = -1;
        private const int k_InvalidTime = 0;
        private const int k_MinCurrentValue = 0;
        private const int k_MaxCurrentValue = 200;
        private const int k_UpdateComPortsIntervalMilliseconds = 5000;
        private const int k_UpdateCurrentVoltageIntervalMilliseconds = 100;

        private readonly PowerSupplyTimerManager _powerSupplyTimerManager;
        private readonly ComPortUpdateTimerManager _comPortUpdateTimerManager;
        private readonly CurrentVoltageUpdateTimerManager _currentVoltageUpdateTimerManager;
        private readonly ComPortManager _powerSupplyComPortManager;
        private readonly ComPortManager _stepMotorComPortManager;
        private readonly PowerSupplyManager _powerSupplyManager;
        private readonly StepMotorManager _stepMotorManager;
        private readonly UIHelper _uiHelper;

        private double currentValue { get; set; }
        private ushort voltageValue = 6;

        public MainWindow()
        {
            InitializeComponent();

            _uiHelper = new UIHelper(Vaporizer, SystemStateLabel, VaporizerButtonBase, VaporizerButtonInside, Indicator, CurrentValueLabel, VoltageValueLabel);
            _powerSupplyTimerManager = new PowerSupplyTimerManager(TimerTextBoxMins, TimerTextBoxSecs, PowerSupplyTurnOff);
            _comPortUpdateTimerManager = new ComPortUpdateTimerManager(UpdateComPorts, k_UpdateComPortsIntervalMilliseconds);
            _currentVoltageUpdateTimerManager = new CurrentVoltageUpdateTimerManager(PowerSupplyUpdateCurrentVoltage, k_UpdateCurrentVoltageIntervalMilliseconds);
            _powerSupplyComPortManager = new ComPortManager(PowerSupplyComPortComboBox);
            _stepMotorComPortManager = new ComPortManager(ShutterComPortComboBox);
            _powerSupplyManager = new PowerSupplyManager(CurrentValueLabel, VoltageValueLabel);
            _stepMotorManager = new StepMotorManager(_uiHelper);

            PowerSupplyComPortComboBox.SelectionChanged += ComboBox_SelectionChanged;
            ShutterComPortComboBox.SelectionChanged += ComboBox_SelectionChanged;

            _comPortUpdateTimerManager.Start();

            // If shutter opened when program started - change icon.
            if (_stepMotorManager.IsShutterOpened())
                _uiHelper.SetShutterImageToOpened();
            else
                _uiHelper.SetShutterImageToClosed();

            // If text boxes for the timer are empty - assume that it means 0 mins 0 secs what equals to direct timer countdown
            if (TimerTextBoxMins.Text == "" && TimerTextBoxSecs.Text == "")
                StartButton.IsEnabled = true;
            else
                StartButton.IsEnabled = false;
        }

        private void UpdateComPorts()
        {
            _powerSupplyComPortManager.PopulateComPortComboBox(ShutterComPortComboBox);
            _stepMotorComPortManager.PopulateComPortComboBox(PowerSupplyComPortComboBox);
        }

        private void PowerSupplyUpdateCurrentVoltage()
        {
            ExecuteWithErrorHandling(() =>
            {
                _powerSupplyManager.ReadCurrentVoltageAndChangeTextBox();
            });
        }

        private void TimerTextBoxMins_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (int.TryParse(textBox.Text, out int minutes) && minutes >= 0)
                {
                    textBox.ClearValue(Border.BorderBrushProperty);
                    textBox.ClearValue(Border.BorderThicknessProperty);
                    textBox.ToolTip = "Введите значение в минутах";
                }
                else
                {
                    textBox.BorderBrush = new SolidColorBrush(Colors.Red);
                    textBox.BorderThickness = new Thickness(1);
                    textBox.ToolTip = $"Введите корректное значение в минутах";
                    StartButton.IsEnabled = true;
                }
            }
        }

        private void TimerTextBoxSecs_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (int.TryParse(textBox.Text, out int seconds) && seconds >= 0)
                {
                    textBox.ClearValue(Border.BorderBrushProperty);
                    textBox.ClearValue(Border.BorderThicknessProperty);
                    textBox.ToolTip = "Введите значение в секундах";
                }
                else
                {
                    textBox.BorderBrush = new SolidColorBrush(Colors.Red);
                    textBox.BorderThickness = new Thickness(1);
                    textBox.ToolTip = $"Введите корректное значение в секундах";
                    StartButton.IsEnabled = true;
                }
            }
        }

        private void TimerTextBoxMins_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void TimerTextBoxSecs_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
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
            _comPortUpdateTimerManager.Stop();
            _currentVoltageUpdateTimerManager.Stop();
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
            if (sender == PowerSupplyComPortComboBox)
                _stepMotorComPortManager.PopulateComPortComboBox(PowerSupplyComPortComboBox);
            else if (sender == ShutterComPortComboBox)
                _powerSupplyComPortManager.PopulateComPortComboBox(ShutterComPortComboBox);
        }

        private void PowerSupplyConnect()
        {
            ExecuteWithErrorHandling(() =>
            {
                if (!_powerSupplyComPortManager.CheckComPort())
                    return;

                string comPort = _powerSupplyComPortManager.GetComPortName();
                _powerSupplyManager.Connect(comPort);
                _currentVoltageUpdateTimerManager.Start();
            }, UncheckVaporizerButton);
        }

        private void PowerSupplyTurnOn()
        {
            ExecuteWithErrorHandling(() =>
            {
                string comPort = _powerSupplyComPortManager.GetComPortName();
                _powerSupplyManager.TurnOn(comPort);
                _currentVoltageUpdateTimerManager.Start();
            }, UncheckVaporizerButton);
        }

        private void PowerSupplyReset()
        {
            ExecuteWithErrorHandling(() =>
            {
                string comPort = _powerSupplyComPortManager.GetComPortName();
                _powerSupplyManager.Reset(comPort);
            }, UncheckVaporizerButton);
        }

        private void PowerSupplyApplyVoltage()
        {
            ExecuteWithErrorHandling(() =>
            {
                _powerSupplyManager.ApplyVoltage(currentValue, voltageValue);
            }, () =>
            {
                PowerSupplyTurnOff();
                UncheckVaporizerButton();
            });
        }

        private void PowerSupplyTurnOff()
        {
            ExecuteWithErrorHandling(() =>
            {
                string comPort = _powerSupplyComPortManager.GetComPortName();
                _powerSupplyManager.TurnOff(comPort);
                _currentVoltageUpdateTimerManager.Stop();
            }, UncheckVaporizerButton);
        }

        private void OpenShutter()
        {
            ExecuteWithErrorHandling(() =>
            {
                string comPort = _stepMotorComPortManager.GetComPortName();
                _stepMotorManager.Forward(comPort);
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
                _stepMotorManager.Reverse(comPort);
                SetShutterImageToClosed();
                ColorizeCloseShutterButton();
            }, SetShutterImageToClosed);
        }

        private void StopStepMotor()
        {
            ExecuteWithErrorHandling(() =>
            {
                string comPort = _stepMotorComPortManager.GetComPortName();
                _stepMotorManager.Reverse(comPort);
                ColorizeStopStepMotorButton();
            });
        }

        private void ShowWarning(string message, string title = "Предупреждение") { MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning); }

        private void ShowError(string message, string title = "Ошибка") { MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error); }

        private void CheckVaporizerButton() { _uiHelper.CheckVaporizerButton(); }

        private void UncheckVaporizerButton()
        {
            _uiHelper.UncheckVaporizerButton();
            _powerSupplyTimerManager.ResetTimer();
            _currentVoltageUpdateTimerManager.Stop();
        }

        private void SetShutterImageToClosed() { _uiHelper.SetShutterImageToClosed(); ; }

        private void SetShutterImageToOpened() { _uiHelper.SetShutterImageToOpened(); }

        private void CurrentSetPointTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                string newText = textBox.Text.Replace(',', '.');
                if (double.TryParse(newText,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out double value) && (value >= k_MinCurrentValue && value <= k_MaxCurrentValue))
                {
                    textBox.ClearValue(Border.BorderBrushProperty);
                    textBox.ClearValue(Border.BorderThicknessProperty);
                    StartButton.IsEnabled = true;
                    currentValue = value;
                    textBox.ToolTip = $"Введите значение от {k_MinCurrentValue} до {k_MaxCurrentValue}А";
                }
                else
                {
                    textBox.BorderBrush = new SolidColorBrush(Colors.Red);
                    textBox.BorderThickness = new Thickness(1);
                    StartButton.IsEnabled = false;
                    textBox.ToolTip = $"Неверное значение. Допустимый диапазон: {k_MinCurrentValue}-{k_MaxCurrentValue}А";
                }
            }
        }
        private void CurrentSetPointTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) { e.Handled = !IsValidInput(e.Text); }

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

        /// Main functions
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!AreComPortsValid())
                return;

            if (!_powerSupplyManager.IsConnected())
            {
                ShowWarning("Отсутствует связь с блоком питания. Проверьте питание на БП и подключение кабеля RS-432");
                return;
            }

            _uiHelper.CustomizeSystemStateLabel("Система работает", Colors.Green);

            try
            {
                PowerSupplyTurnOn();
                PowerSupplyApplyVoltage();
                PowerSupplyUpdateCurrentVoltage(); // Reads specific register for the current and voltage and updating labels in UI
                PowerSupplyReset(); // Resets specific register that needed to correctly manage power supply after rebooting

                StartCountdown(); // Starting countdown of the spraying time timer
                StartButton.IsEnabled = false;
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void VaporizerButtonBase_Checked(object sender, RoutedEventArgs e)
        {
            if (_powerSupplyManager.IsConnected())
                return;
            PowerSupplyConnect();
            CheckVaporizerButton();
        }

        private void VaporizerButtonBase_Unchecked(object sender, RoutedEventArgs e)
        {
            if (!_powerSupplyManager.IsConnected())
                return;
            PowerSupplyTurnOff();
            UncheckVaporizerButton();
            _powerSupplyTimerManager.ResetTimer();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_powerSupplyManager.IsConnected())
                PowerSupplyTurnOff();
            if (_stepMotorManager.IsConnected())
                StopStepMotor();
        }

        private void AddScenarioButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
