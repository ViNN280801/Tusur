using System.Windows.Controls;
using System.Windows.Threading;
using TusurUI.Interfaces;

public class TimerManager : ITimerManager
{
    private DispatcherTimer? _timerCountdown; // Timer for the power supply (power supply turned off when timer is 0)
    private int _remainingSeconds;
    private bool _isDirectCountdown = false;
    private TextBox _timerTextBox;

    public TimerManager(TextBox timerTextBox)
    {
        _timerTextBox = timerTextBox ?? throw new ArgumentNullException(nameof(timerTextBox));
    }

    public void StartCountdown(TextBox timerTextBox)
    {
        if (string.IsNullOrWhiteSpace(timerTextBox.Text))
        {
            // Direct countdown if timer text box is empty
            _remainingSeconds = 0;
            _isDirectCountdown = true;
        }
        else if (int.TryParse(timerTextBox.Text, out int minutes) && minutes > 0)
        {
            // Reverse countdown if timer text box isn't empty
            _remainingSeconds = minutes * 60;
            timerTextBox.IsReadOnly = true;
            _isDirectCountdown = false;
        }
        else
        {
            throw new ArgumentException("Введите корректное значение в минутах.");
        }

        _timerCountdown = new DispatcherTimer();
        _timerCountdown.Interval = TimeSpan.FromSeconds(1);
        _timerCountdown.Tick += TimerCountdown_Tick;
        _timerCountdown.Start();
    }

    public void ResetTimer()
    {
        _timerCountdown?.Stop();
        if (_timerTextBox != null)
        {
            _timerTextBox.IsReadOnly = false;
            _timerTextBox.Text = string.Empty;
        }
    }

    public bool IsValidTimerInput(string text) { return text.All(char.IsDigit); }

    private void TimerCountdown_Tick(object? sender, EventArgs e)
    {
        if (_timerTextBox == null)
            return;

        if (_isDirectCountdown)
        {
            _remainingSeconds++;
            _timerTextBox.Text = $"{_remainingSeconds / 60}:{_remainingSeconds % 60:D2}";
        }
        else
        {
            if (_remainingSeconds > 0)
            {
                _remainingSeconds--;
                _timerTextBox.Text = $"{_remainingSeconds / 60}:{_remainingSeconds % 60:D2}";
            }
            else
            {
                ResetTimer();
            }
        }
    }
}
