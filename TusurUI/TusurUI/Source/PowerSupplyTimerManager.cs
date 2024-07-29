using System.Windows.Controls;
using System.Windows.Threading;
using TusurUI.Interfaces;

// The class is needed in order to set the operating time of the power supply. It works in 2 modes:
// 1) Direct countdown
// 2) Countdown
// If the user has set a certain time value in the textbox, the timer will work on the countdown. 
// Therefore, the power supply will turn off after the specified time. If the time has not been set, 
// the textbox will simply serve as a timer so that the user can see how long the power supply is running.
public class PowerSupplyTimerManager : IPowerSupplyTimerManager
{
    private readonly TextBox _timerTextBoxMins;
    private readonly TextBox _timerTextBoxSecs;
    private DispatcherTimer? _timerCountdown; // Timer for the power supply (power supply turned off when timer is 0)
    private int _remainingSeconds;
    private bool _isDirectCountdown = false;
    private readonly Action _turnOffPowerSupply;

    public PowerSupplyTimerManager(TextBox timerTextBoxMins, TextBox timerTextBoxSecs, Action turnOffPowerSupply)
    {
        _timerTextBoxMins = timerTextBoxMins ?? throw new ArgumentNullException(nameof(timerTextBoxMins));
        _timerTextBoxSecs = timerTextBoxSecs ?? throw new ArgumentNullException(nameof(timerTextBoxSecs));
        _turnOffPowerSupply = turnOffPowerSupply ?? throw new ArgumentNullException(nameof(turnOffPowerSupply));
    }

    public void StartCountdown()
    {
        bool minsEmpty = string.IsNullOrWhiteSpace(_timerTextBoxMins.Text);
        bool secsEmpty = string.IsNullOrWhiteSpace(_timerTextBoxSecs.Text);

        if (minsEmpty && secsEmpty)
        {
            _remainingSeconds = 0;
            _isDirectCountdown = true;
            _timerTextBoxMins.IsReadOnly = true;
            _timerTextBoxSecs.IsReadOnly = true;
        }
        else
        {
            if (!int.TryParse(_timerTextBoxMins.Text, out int minutes) || minutes < 0)
            {
                throw new ArgumentException("Введите корректное значение в минутах.");
            }

            if (!int.TryParse(_timerTextBoxSecs.Text, out int seconds) || seconds < 0)
            {
                throw new ArgumentException("Введите корректное значение в секундах.");
            }

            _remainingSeconds = (minutes * 60) + seconds;

            if (_remainingSeconds > 0)
            {
                _timerTextBoxMins.IsReadOnly = true;
                _timerTextBoxSecs.IsReadOnly = true;
                _isDirectCountdown = false;
            }
            else
            {
                _remainingSeconds = 0;
                _isDirectCountdown = true;
            }
        }

        _timerCountdown = new DispatcherTimer();
        _timerCountdown.Interval = TimeSpan.FromSeconds(1);
        _timerCountdown.Tick += TimerCountdown_Tick;
        _timerCountdown.Start();
    }

    public void ResetTimer()
    {
        _timerCountdown?.Stop();
        _timerTextBoxMins.IsReadOnly = false;
        _timerTextBoxMins.Text = string.Empty;
        _timerTextBoxSecs.IsReadOnly = false;
        _timerTextBoxSecs.Text = string.Empty;
    }

    public bool IsValidTimerInput(string text) { return int.TryParse(text, out int value) && value >= 0; }

    private void TimerCountdown_Tick(object? sender, EventArgs e)
    {
        if (_isDirectCountdown)
        {
            _remainingSeconds++;
        }
        else
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
            }
            else
            {
                _timerCountdown?.Stop();
                _turnOffPowerSupply?.Invoke();
                ResetTimer();
                return;
            }
        }

        int minutes = _remainingSeconds / 60;
        int seconds = _remainingSeconds % 60;

        _timerTextBoxMins.Text = minutes.ToString();
        _timerTextBoxSecs.Text = seconds.ToString();
    }
}
