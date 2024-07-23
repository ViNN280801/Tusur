using System.Windows.Controls;

namespace TusurUI.Interfaces
{
    public interface IPowerSupplyTimerManager
    {
        void StartCountdown(TextBox timerTextBox);
        void ResetTimer();
    }
}
