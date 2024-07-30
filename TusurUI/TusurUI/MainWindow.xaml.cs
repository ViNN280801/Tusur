using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TusurUI.Source;
using TusurUI.Helpers;

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

        public readonly ComPortUpdateTimerManager _comPortUpdateTimerManager;
        public readonly CurrentVoltageUpdateTimerManager _currentVoltageUpdateTimerManager;
        public readonly ComPortManager _powerSupplyComPortManager;
        public readonly ComPortManager _stepMotorComPortManager;
        public readonly PowerSupplyManager _powerSupplyManager;
        public readonly StepMotorManager _stepMotorManager;
        private readonly UIHelper _uiHelper;

        public MainWindow()
        {
            InitializeComponent();

            _uiHelper = new UIHelper(Vaporizer, SystemStateLabel, VaporizerButtonBase, VaporizerButtonInside, Indicator, CurrentValueLabel, VoltageValueLabel);
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

        private void ShowSuccess(string message, string title = "Успех", int stageNumber = -1)
        {
            if (stageNumber > 0)
            {
                MessageBox.Show($"Стадия №{stageNumber} успешно завершена", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CheckVaporizerButton() { _uiHelper.CheckVaporizerButton(); }

        public void UncheckVaporizerButton()
        {
            _uiHelper.UncheckVaporizerButton();
            _currentVoltageUpdateTimerManager.Stop();
        }

        private void SetShutterImageToClosed() { _uiHelper.SetShutterImageToClosed(); ; }
        private void SetShutterImageToOpened() { _uiHelper.SetShutterImageToOpened(); }
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

        public void PowerSupplyConnect()
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

        public void PowerSupplyTurnOn()
        {
            ExecuteWithErrorHandling(() =>
            {
                string comPort = _powerSupplyComPortManager.GetComPortName();
                _powerSupplyManager.TurnOn(comPort);
                _currentVoltageUpdateTimerManager.Start();
            }, UncheckVaporizerButton);
        }

        public void PowerSupplyReset()
        {
            ExecuteWithErrorHandling(() =>
            {
                string comPort = _powerSupplyComPortManager.GetComPortName();
                _powerSupplyManager.Reset(comPort);
            }, UncheckVaporizerButton);
        }

        public void PowerSupplyApplyVoltage(ushort current)
        {
            ExecuteWithErrorHandling(() =>
            {
                _powerSupplyManager.ApplyVoltage(current, PowerSupply.kdefault_Voltage);
            }, () =>
            {
                PowerSupplyTurnOff();
                UncheckVaporizerButton();
            });
        }

        public void PowerSupplyTurnOff()
        {
            ExecuteWithErrorHandling(() =>
            {
                string comPort = _powerSupplyComPortManager.GetComPortName();
                _powerSupplyManager.TurnOff(comPort);
                _currentVoltageUpdateTimerManager.Stop();
            }, UncheckVaporizerButton);
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
        private void AddScenarioButton_Click(object sender, RoutedEventArgs e)
        {
            ScenariosWindow scenariosWindow = new ScenariosWindow(this);
            scenariosWindow.Show();
            AddScenarioButton.IsEnabled = false;
        }
        private void StartButton_Click(object sender, RoutedEventArgs e) // TODO: move this logic to the new window
        {
            //if (!AreComPortsValid())
            //    return;

            //if (!_powerSupplyManager.IsConnected())
            //{
            //    ShowWarning("Отсутствует связь с блоком питания. Проверьте питание на БП и подключение кабеля RS-432");
            //    return;
            //}

            _uiHelper.CustomizeSystemStateLabel("Система работает", Colors.Green);

            try
            {
                //PowerSupplyTurnOn();
                //PowerSupplyApplyVoltage();
                //PowerSupplyUpdateCurrentVoltage(); // Reads specific register for the current and voltage and updating labels in UI
                //PowerSupplyReset(); // Resets specific register that needed to correctly manage power supply after rebooting
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
        }

        private void VaporizerButtonBase_Checked(object sender, RoutedEventArgs e)
        {
            //if (_powerSupplyManager.IsConnected())
            //    return;
            //PowerSupplyConnect();
            CheckVaporizerButton();
        }

        private void VaporizerButtonBase_Unchecked(object sender, RoutedEventArgs e)
        {
            //if (!_powerSupplyManager.IsConnected())
            //    return;
            //PowerSupplyTurnOff();
            UncheckVaporizerButton();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_powerSupplyManager.IsConnected())
                PowerSupplyTurnOff();
            if (_stepMotorManager.IsConnected())
                StopStepMotor();
        }
    }
}
