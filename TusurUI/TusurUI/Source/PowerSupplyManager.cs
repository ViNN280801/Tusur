using System.Windows.Controls;
using TusurUI.Interfaces;

namespace TusurUI.Source
{
    /**
     * @class PowerSupplyManager
     * @brief Manages the power supply in the application.
     *
     * This class provides functionality to control the power supply, including connecting, turning on,
     * resetting, applying voltage, reading current and voltage, and turning off the power supply.
     */
    public class PowerSupplyManager : IPowerSupplyManager
    {
        private readonly Label _currentValueLabel;
        private readonly Label _voltageValueLabel;

        public PowerSupplyManager(Label currentValueLabel, Label voltageValueLabel)
        {
            _currentValueLabel = currentValueLabel ?? throw new ArgumentNullException(nameof(currentValueLabel));
            _voltageValueLabel = voltageValueLabel ?? throw new ArgumentNullException(nameof(voltageValueLabel));
        }

        public void ConnectToPowerSupply(string comPort) { PowerSupply.Connect(comPort); }

        public void TurnOnPowerSupply(string comPort)
        {
            ConnectToPowerSupply(comPort);
            ExecuteCommand(PowerSupply.TurnOn);
        }

        public void Reset(string comPort)
        {
            ConnectToPowerSupply(comPort);
            ExecuteCommand(PowerSupply.Reset);
        }

        public void ApplyVoltageOnPowerSupply(double currentValue, ushort voltageValue)
        {
            ExecuteCommand(() => PowerSupply.SetCurrentVoltage((ushort)currentValue, voltageValue));
        }

        public void ReadCurrentVoltageAndChangeTextBox()
        {
            int current = PowerSupply.ReadCurrent();
            if (current == -1)
                throw new Exception(PowerSupply.GetErrorMessage(current));

            _currentValueLabel.Content = current.ToString() + " A";

            int voltage = PowerSupply.ReadVoltage();
            if (voltage == -2)
                throw new Exception(PowerSupply.GetErrorMessage(voltage));

            // Convert the voltage to float by dividing by 100
            float voltageFloat = voltage / 100.0f;
            _voltageValueLabel.Content = voltageFloat.ToString("F2") + " В";
        }

        public void TurnOffPowerSupply(string comPort)
        {
            ConnectToPowerSupply(comPort);
            ExecuteCommand(PowerSupply.TurnOff);
        }

        private void ExecuteCommand(Func<int> command)
        {
            string? err = GetErrorMessage(command());
            if (err != null)
                throw new Exception(err);
        }

        private string? GetErrorMessage(int errorCode)
        {
            if (errorCode > 0)
                return PowerSupply.GetErrorMessage(errorCode);
            return null;
        }
    }
}
