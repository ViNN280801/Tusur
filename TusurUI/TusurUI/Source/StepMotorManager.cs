﻿using TusurUI.ExternalSources;
using TusurUI.Helpers;
using TusurUI.Interfaces;

namespace TusurUI.Source
{
    /**
     * @class StepMotorManager
     * @brief Manages the step motor in the application.
     *
     * This class provides functionality to control the step motor, including opening, closing,
     * and stopping the shutter, as well as checking the shutter's status.
     */
    public class StepMotorManager : IStepMotorManager
    {
        private readonly UIHelper _uiHelper;

        public StepMotorManager(UIHelper uiHelper)
        {
            _uiHelper = uiHelper ?? throw new ArgumentNullException(nameof(uiHelper));
        }

        public void OpenShutter(string comPort)
        {
            StepMotor.Connect(comPort);
            ExecuteCommand(StepMotor.Forward);
            _uiHelper.SetShutterImageToOpened();
        }

        public void CloseShutter(string comPort)
        {
            StepMotor.Connect(comPort);
            ExecuteCommand(StepMotor.Reverse);
            _uiHelper.SetShutterImageToClosed();
        }

        public void StopStepMotor(string comPort)
        {
            StepMotor.Connect(comPort);
            ExecuteCommand(StepMotor.Stop);
        }

        public bool IsShutterOpened()
        {
            return ComponentManager.GetIndicatorImage(_uiHelper.Vaporizer)?.UriSource.ToString().Contains("откр") ?? false;
        }

        public bool IsShutterClosed()
        {
            return ComponentManager.GetIndicatorImage(_uiHelper.Vaporizer)?.UriSource.ToString().Contains("закр") ?? false;
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
                return StepMotor.GetErrorMessage(errorCode);
            return null;
        }
    }
}
