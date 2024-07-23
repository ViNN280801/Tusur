using System.IO.Ports;
using System.Windows.Controls;
using TusurUI.Interfaces;

namespace TusurUI.Source
{
    /**
     * @class ComPortManager
     * @brief Manages the COM ports in the application.
     *
     * This class provides functionality to populate a ComboBox with available COM ports,
     * check the validity of the selected COM port, and retrieve the name of the selected COM port.
     */
    public class ComPortManager : IComPortManager
    {
        private readonly ComboBox _comboBox;
        public ComPortManager(ComboBox comboBox)
        {
            _comboBox = comboBox ?? throw new ArgumentNullException(nameof(comboBox));
            PopulateComPortComboBox();
        }

        public void PopulateComPortComboBox()
        {
            string[] ports = SerialPort.GetPortNames();
            _comboBox.ItemsSource = ports;

            if (ports.Length > 0)
            {
                if (!_comboBox.Items.Contains(_comboBox.SelectedItem))
                    _comboBox.SelectedIndex = 0;
                _comboBox.IsEnabled = true;
            }
            else
                _comboBox.IsEnabled = false;
        }
        public bool CheckComPort()
        {
            if (IsComPortNotSelected())
                throw new ArgumentNullException("Internal Error: COM-port is empty.");
            return true;
        }

        public bool IsComPortSelected() { return (_comboBox.SelectedItem != null); }
        public bool IsComPortNotSelected() { return (_comboBox.SelectedItem == null); }

        public string GetComPortName()
        {
            string? comPort = _comboBox.SelectedItem.ToString();
            if (comPort == null)
                throw new ArgumentNullException("Internal Error: COM-port is empty.");
            return comPort;
        }
    }
}
