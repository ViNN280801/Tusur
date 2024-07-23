using System.Windows.Controls;

namespace TusurUI.Interfaces
{
    public interface ITimerManager
    {
        void StartCountdown(TextBox timerTextBox);
        void ResetTimer();
    }
}
