using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using TusurUI.Source;

namespace TusurUI.Helpers
{
    /**
     * @class UIHelper
     * @brief Provides helper methods for interacting with the UI components.
     *
     * This class contains methods to update and manipulate various UI components such as
     * images, labels, buttons, and indicators.
     */
    public class UIHelper
    {
        private readonly Image _vaporizer;
        private readonly Label _systemStateLabel;
        private readonly ToggleButton _vaporizerButtonBase;
        private readonly ToggleButton _vaporizerButtonInside;
        private readonly Image _indicator;
        private readonly Label _currentValueLabel;
        private readonly Label _voltageValueLabel;

        public Image Vaporizer => _vaporizer;

        public UIHelper(Image vaporizer, Label systemStateLabel, ToggleButton vaporizerButtonBase, ToggleButton vaporizerButtonInside, Image indicator, Label currentValueLabel, Label voltageValueLabel)
        {
            _vaporizer = vaporizer ?? throw new ArgumentNullException(nameof(vaporizer));
            _systemStateLabel = systemStateLabel ?? throw new ArgumentNullException(nameof(systemStateLabel));
            _vaporizerButtonBase = vaporizerButtonBase ?? throw new ArgumentNullException(nameof(vaporizerButtonBase));
            _vaporizerButtonInside = vaporizerButtonInside ?? throw new ArgumentNullException(nameof(vaporizerButtonInside));
            _indicator = indicator ?? throw new ArgumentNullException(nameof(indicator));
            _currentValueLabel = currentValueLabel ?? throw new ArgumentNullException(nameof(currentValueLabel));
            _voltageValueLabel = voltageValueLabel ?? throw new ArgumentNullException(nameof(voltageValueLabel));
        }

        public Image GetImage() { return _vaporizer; }

        public void CheckVaporizerButton()
        {
            _vaporizerButtonBase.Background = new SolidColorBrush(Colors.Green);
            DockPanel.SetDock(_vaporizerButtonInside, Dock.Right);
            ComponentManager.ChangeIndicatorPicture(_indicator, "Images/индикатор вкл.jpg");
        }

        public void UncheckVaporizerButton()
        {
            _systemStateLabel.Content = "Система не работает";
            _systemStateLabel.Foreground = new SolidColorBrush(Colors.Red);
            _vaporizerButtonBase.Background = new SolidColorBrush(Colors.White);
            DockPanel.SetDock(_vaporizerButtonInside, Dock.Left);
            ComponentManager.ChangeIndicatorPicture(_indicator, "Images/индикатор откл.jpg");

            _currentValueLabel.Content = "0 A";
            _voltageValueLabel.Content = "0 В";
        }

        public void SetShutterImageToClosed()
        {
            ComponentManager.ChangeIndicatorPicture(_vaporizer, "Images/заслонка закр фото.png");
        }

        public void SetShutterImageToOpened()
        {
            ComponentManager.ChangeIndicatorPicture(_vaporizer, "Images/заслонка откр.png");
        }

        public void ColorizeOpenShutterButton(Button openShutterButton, Button closeShutterButton, Button stopStepMotorButton)
        {
            stopStepMotorButton.Background = new SolidColorBrush(Colors.White);
            closeShutterButton.Background = new SolidColorBrush(Colors.White);
            openShutterButton.Background = new SolidColorBrush(Colors.Green);
        }

        public void ColorizeCloseShutterButton(Button openShutterButton, Button closeShutterButton, Button stopStepMotorButton)
        {
            stopStepMotorButton.Background = new SolidColorBrush(Colors.White);
            closeShutterButton.Background = new SolidColorBrush(Colors.Red);
            openShutterButton.Background = new SolidColorBrush(Colors.White);
        }

        public void ColorizeStopStepMotorButton(Button openShutterButton, Button closeShutterButton, Button stopStepMotorButton)
        {
            stopStepMotorButton.Background = new SolidColorBrush(Colors.Gray);
            closeShutterButton.Background = new SolidColorBrush(Colors.White);
            openShutterButton.Background = new SolidColorBrush(Colors.White);
        }
    }
}
