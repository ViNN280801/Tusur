using System.Windows.Threading;
using TusurUI.Interfaces;

namespace TusurUI.Source
{
    /**
     * @class ComPortUpdateTimerManager
     * @brief Manages the timer for updating the list of COM ports at regular intervals.
     *
     * This class implements the IBaseTimerManager interface to manage a DispatcherTimer
     * that updates the list of available COM ports at specified intervals.
     */
    public class ComPortUpdateTimerManager : IBaseTimerManager
    {
        private readonly DispatcherTimer _timer;
        private readonly Action _updateAction;

        /**
         * @brief Constructor for ComPortUpdateTimerManager.
         * @param updateAction The action to be performed at each timer interval.
         * @param intervalMilliseconds The interval in milliseconds at which the timer will execute the action.
         *
         * Initializes the timer with the specified update action and interval.
         */
        public ComPortUpdateTimerManager(Action updateAction, int intervalMilliseconds)
        {
            _updateAction = updateAction ?? throw new ArgumentNullException(nameof(updateAction));
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(intervalMilliseconds)
            };
            _timer.Tick += (sender, args) => _updateAction();
        }

        /**
         * @brief Starts the timer.
         *
         * This method starts the timer, causing it to execute the update action at regular intervals.
         */
        public void Start() => _timer.Start();

        /**
         * @brief Stops the timer.
         *
         * This method stops the timer, preventing it from executing any further actions.
         */
        public void Stop() => _timer.Stop();
    }
}
