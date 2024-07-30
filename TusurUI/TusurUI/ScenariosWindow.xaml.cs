using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections.Generic;
using System.Threading.Tasks;
using TusurUI.Helpers;
using TusurUI.Source;

namespace TusurUI
{
    public partial class ScenariosWindow : Window
    {
        private const ushort k_nullCurrent = 0;
        private const ushort k_MaxCurrent = 200;
        private const ushort k_nullTime = 0;
        private const ushort k_MaxHours = 24;
        private const ushort k_MaxMinutes = 60;
        private const ushort k_MaxSeconds = 60;

        private const uint k_SecondsInDay = 86400;
        private const ushort k_SecondsInHours = 3600;
        private const ushort k_SecondsInMinutes = 60;

        private MainWindow _mainWindow;
        private PowerSupplyTimerManager _powerSupplyTimerManager;

        public ScenariosWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _powerSupplyTimerManager = new PowerSupplyTimerManager(GetTextBoxByName("TimerHoursTextBox"), GetTextBoxByName("TimerMinutesTextBox"), GetTextBoxByName("TimerSecondsTextBox"), GetProgressBarByName("ProgressBar"), _mainWindow.PowerSupplyTurnOff);
        }

        private void ClearTimerTextBoxes()
        {
            TimerHoursTextBox.Text = "";
            TimerMinutesTextBox.Text = "";
            TimerSecondsTextBox.Text = "";
        }

        private void CountdownModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            ClearTimerTextBoxes();
            SetCountdownModeVisibility(Visibility.Collapsed);
            EnsureSingleRow();
        }
        private void CountdownModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetCountdownModeVisibility(Visibility.Visible);
        }

        private void SetCountdownModeVisibility(Visibility visibility)
        {
            ProgressLabel.Visibility = visibility;
            foreach (var child in ScenarioStackPanel.Children)
            {
                if (child is StackPanel row)
                {
                    foreach (var element in row.Children)
                    {
                        if (element is Button button && (button.Content.ToString() == "+" || button.Content.ToString() == "-"))
                        {
                            button.Visibility = visibility;
                        }
                        else if (element is ProgressBar progressBar)
                        {
                            progressBar.Visibility = visibility;
                        }
                    }
                }
            }
        }

        private void EnsureSingleRow()
        {
            while (ScenarioStackPanel.Children.Count > 2)
            {
                // Remove all rows except the first one
                ScenarioStackPanel.Children.RemoveAt(1);
            }
        }

        private void AddRowButton_Click(object sender, RoutedEventArgs e)
        {
            AddRow();
        }

        private void RemoveRowButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Parent is StackPanel row)
                RemoveRow(row);
        }

        private void AddRow()
        {
            StackPanel row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };

            TextBox currentTextBox = new TextBox { Name = "CurrentTextBox", Width = 66, Height = 25, Style = (Style)FindResource("ScenarioTextBoxStyle") };
            currentTextBox.PreviewTextInput += CurrentTextBox_PreviewTextInput;
            row.Children.Add(currentTextBox);

            Label aLabel = new Label { Content = "A", Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(aLabel);

            TextBox timerTextBoxHours = new TextBox { Name = "TimerTextBoxHours", Width = 50, Height = 25, Style = (Style)FindResource("ScenarioTextBoxStyle") };
            timerTextBoxHours.PreviewTextInput += TimerHoursTextBox_PreviewTextInput;
            row.Children.Add(timerTextBoxHours);

            Label hourLabel = new Label { Content = "час", Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(hourLabel);

            TextBox timerTextBoxMins = new TextBox { Name = "TimerTextBoxMins", Width = 50, Height = 25, Style = (Style)FindResource("ScenarioTextBoxStyle") };
            timerTextBoxMins.PreviewTextInput += TimerMinutesTextBox_PreviewTextInput;
            row.Children.Add(timerTextBoxMins);

            Label minLabel = new Label { Content = "мин", Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(minLabel);

            TextBox timerTextBoxSecs = new TextBox { Name = "TimerTextBoxSecs", Width = 50, Height = 25, Style = (Style)FindResource("ScenarioTextBoxStyle") };
            timerTextBoxSecs.PreviewTextInput += TimerSecondsTextBox_PreviewTextInput;
            row.Children.Add(timerTextBoxSecs);

            Label secLabel = new Label { Content = "с", Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(secLabel);

            ProgressBar progressBar = new ProgressBar { Name = "ProgressBar", Width = 152, Height = 20, Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center, Visibility = Visibility.Visible };
            row.Children.Add(progressBar);

            Button addButton = new Button { Content = "+", Width = 25, Height = 25, Background = new SolidColorBrush(Colors.Green), Style = (Style)FindResource("ScenarioAddRemoveButtonStyle") };
            addButton.Click += AddRowButton_Click;
            row.Children.Add(addButton);

            Button removeButton = new Button { Content = "-", Width = 25, Height = 25, Background = new SolidColorBrush(Colors.Red), Style = (Style)FindResource("ScenarioAddRemoveButtonStyle") };
            removeButton.Click += RemoveRowButton_Click;
            row.Children.Add(removeButton);

            ScenarioStackPanel.Children.Add(row);
        }

        private void RemoveRow(StackPanel row)
        {
            if (ScenarioStackPanel.Children.Count > 2)
                ScenarioStackPanel.Children.Remove(row);
        }

        private void CurrentTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _) || int.Parse(e.Text) < k_nullCurrent || int.Parse(e.Text) > k_MaxCurrent;
        }
        private void CurrentTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!int.TryParse(textBox.Text, out int current) || current <= k_nullCurrent || current > k_MaxCurrent)
                    UIHelper.MarkTextBoxAsInvalid(textBox);
                else
                    UIHelper.RestoreTextBoxStyle(textBox);
            }
        }
        private void TimerHoursTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _) || int.Parse(e.Text) < k_nullTime || int.Parse(e.Text) > k_MaxHours;
        }
        private void TimerHoursTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!int.TryParse(textBox.Text, out int hours) || hours < k_nullTime || hours > k_MaxHours)
                    UIHelper.MarkTextBoxAsInvalid(textBox);
                else
                    UIHelper.RestoreTextBoxStyle(textBox);
            }
        }
        private void TimerMinutesTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _) || int.Parse(e.Text) < k_nullTime || int.Parse(e.Text) > k_MaxMinutes;
        }
        private void TimerMinutesTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!int.TryParse(textBox.Text, out int mins) || mins < k_nullTime || mins > k_MaxMinutes)
                    UIHelper.MarkTextBoxAsInvalid(textBox);
                else
                    UIHelper.RestoreTextBoxStyle(textBox);
            }
        }
        private void TimerSecondsTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _) || int.Parse(e.Text) < k_nullTime || int.Parse(e.Text) > k_MaxSeconds;
        }
        private void TimerSecondsTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!int.TryParse(textBox.Text, out int secs) || secs < k_nullTime || secs > k_MaxSeconds)
                    UIHelper.MarkTextBoxAsInvalid(textBox);
                else
                    UIHelper.RestoreTextBoxStyle(textBox);
            }
        }
        private bool IsCurrentFieldValid(TextBox textBox, ref bool allFieldsAreZero)
        {
            if (!int.TryParse(textBox.Text, out int current) || current <= k_nullCurrent || current > k_MaxCurrent)
            {
                UIHelper.MarkTextBoxAsInvalid(textBox);
                return false;
            }

            if (current != k_nullCurrent)
            {
                allFieldsAreZero = false;
            }

            UIHelper.RestoreTextBoxStyle(textBox);
            return true;
        }
        private bool IsTimerFieldValid(TextBox textBox, ref int totalSeconds, ref bool allFieldsAreZero)
        {
            int value = int.TryParse(textBox.Text, out int parsedValue) ? parsedValue : k_nullTime;

            if (textBox.Name == "TimerHoursTextBox")
            {
                return IsHoursFieldValid(textBox, value, ref totalSeconds, ref allFieldsAreZero);
            }
            else if (textBox.Name == "TimerMinutesTextBox")
            {
                return IsMinutesFieldValid(textBox, value, ref totalSeconds, ref allFieldsAreZero);
            }
            else if (textBox.Name == "TimerSecondsTextBox")
            {
                return IsSecondsFieldValid(textBox, value, ref totalSeconds, ref allFieldsAreZero);
            }

            return true;
        }
        private bool IsHoursFieldValid(TextBox textBox, int value, ref int totalSeconds, ref bool allFieldsAreZero)
        {
            if (value < k_nullTime || value > k_MaxHours)
            {
                UIHelper.MarkTextBoxAsInvalid(textBox);
                return false;
            }

            totalSeconds += value * k_SecondsInHours;
            if (value != k_nullTime)
            {
                allFieldsAreZero = false;
            }

            UIHelper.RestoreTextBoxStyle(textBox);
            return true;
        }
        private bool IsMinutesFieldValid(TextBox textBox, int value, ref int totalSeconds, ref bool allFieldsAreZero)
        {
            if (value < k_nullTime || value > k_MaxMinutes)
            {
                UIHelper.MarkTextBoxAsInvalid(textBox);
                return false;
            }

            totalSeconds += value * k_SecondsInMinutes;
            if (value != k_nullTime)
            {
                allFieldsAreZero = false;
            }

            UIHelper.RestoreTextBoxStyle(textBox);
            return true;
        }
        private bool IsSecondsFieldValid(TextBox textBox, int value, ref int totalSeconds, ref bool allFieldsAreZero)
        {
            if (value < k_nullTime || value > k_MaxSeconds)
            {
                UIHelper.MarkTextBoxAsInvalid(textBox);
                return false;
            }

            totalSeconds += value;
            if (value != k_nullTime)
            {
                allFieldsAreZero = false;
            }

            UIHelper.RestoreTextBoxStyle(textBox);
            return true;
        }
        private bool AreFieldsValid(out List<string> missingFields)
        {
            int totalSeconds = k_nullTime;
            bool allFieldsAreZero = true;
            missingFields = new List<string>();

            foreach (var element in ScenarioControlsStackPanel.Children)
            {
                if (element is TextBox textBox)
                {
                    if (textBox.Name == "CurrentTextBox")
                    {
                        if (!IsCurrentFieldValid(textBox, ref allFieldsAreZero))
                        {
                            missingFields.Add("Величина тока");
                        }
                    }

                    if (!IsTimerFieldValid(textBox, ref totalSeconds, ref allFieldsAreZero))
                    {
                        if (textBox.Name == "TimerHoursTextBox")
                        {
                            missingFields.Add("Часы");
                        }
                        else if (textBox.Name == "TimerMinutesTextBox")
                        {
                            missingFields.Add("Минуты");
                        }
                        else if (textBox.Name == "TimerSecondsTextBox")
                        {
                            missingFields.Add("Секунды");
                        }
                    }
                }
            }

            if (allFieldsAreZero)
            {
                MessageBox.Show($"Невозможно применить сценарий со всеми нулевыми полями.", "Проверка полей", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (totalSeconds >= k_SecondsInDay)
            {
                MessageBox.Show($"Общее время не может превышать 24 часа.", "Проверка введенного времени", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return missingFields.Count == 0;
        }

        private bool IsDirectCountdown() { return CountdownModeCheckBox.IsChecked == true; }
        private bool IsReverseCountdown() { return CountdownModeCheckBox.IsChecked == false; }

        private async Task StartTimerAsync(bool isCountdown)
        {
            try
            {
                _powerSupplyTimerManager.StartCountdown(isCountdown);

                await Task.Run(() =>
                {
                    while (_powerSupplyTimerManager.IsRunning) { /* Waiting for the timer to complete */ }
                });

                MessageBox.Show("Стадия симуляции завершена успешно.", "Завершение сценариев", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Internal Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetTextBoxesEnabled(bool isEnabled)
        {
            foreach (var element in ScenarioControlsStackPanel.Children)
            {
                if (element is TextBox textBox)
                {
                    textBox.IsEnabled = isEnabled;
                }
            }
        }
        private void SetAddRemoveButtonsEnabled(bool isEnabled)
        {
            foreach (var element in ScenarioControlsStackPanel.Children)
            {
                if (element is Button button)
                {
                    button.IsEnabled = isEnabled;
                }
            }
        }

        private TimeSpan GetTotalTime(StackPanel panel)
        {
            int hours = 0, minutes = 0, seconds = 0;

            foreach (var element in panel.Children)
            {
                if (element is TextBox textBox)
                {
                    if (textBox.Name == "TimerHoursTextBox")
                    {
                        hours = int.TryParse(textBox.Text, out int h) ? h : 0;
                    }
                    else if (textBox.Name == "TimerMinutesTextBox")
                    {
                        minutes = int.TryParse(textBox.Text, out int m) ? m : 0;
                    }
                    else if (textBox.Name == "TimerSecondsTextBox")
                    {
                        seconds = int.TryParse(textBox.Text, out int s) ? s : 0;
                    }
                }
            }

            return new TimeSpan(hours, minutes, seconds);
        }

        private TextBox GetTextBoxByName(string name)
        {
            foreach (var element in ScenarioControlsStackPanel.Children)
            {
                if (element is TextBox textBox && textBox.Name == name)
                {
                    return textBox;
                }
            }
            throw new Exception($"Internal Error: TextBox with name '{name}' not found.");
        }

        private ProgressBar GetProgressBarByName(string name)
        {
            foreach (var element in ScenarioControlsStackPanel.Children)
            {
                if (element is ProgressBar progressBar && progressBar.Name == name)
                {
                    return progressBar;
                }
            }
            throw new Exception($"Internal Error: ProgressBar with name '{name}' not found.");
        }

        private void WriteLogFile(bool isDirectCountdown, TimeSpan initialTime, int stageNumber, StackPanel row)
        {
            string directory = "scenarios";
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}_scenario_params.log";
            string filePath = Path.Combine(directory, fileName);

            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine($"Стадия №{stageNumber}:");

                foreach (var element in row.Children)
                {
                    if (element is TextBox textBox && textBox.Name == "CurrentTextBox")
                    {
                        writer.WriteLine($"    Величина тока: {textBox.Text}");
                    }
                }

                writer.WriteLine($"    Мод таймера: {(isDirectCountdown ? "Прямой отсчет" : "Обратный отсчет")}");
                if (!isDirectCountdown)
                {
                    writer.WriteLine("    Настройки таймера:");
                    writer.WriteLine($"        Часы: {initialTime.Hours}");
                    writer.WriteLine($"        Минуты: {initialTime.Minutes}");
                    writer.WriteLine($"        Секунды: {initialTime.Seconds}");
                }
            }
        }

        private void OnStartDisableUI()
        {
            SetTextBoxesEnabled(false);
            SetAddRemoveButtonsEnabled(false);
            CountdownModeCheckBox.IsEnabled = false;
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
        }

        private void OnStopEnableUI()
        {
            SetTextBoxesEnabled(true);
            SetAddRemoveButtonsEnabled(true);
            CountdownModeCheckBox.IsEnabled = true;
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
        }

        private void FinalizeApplication()
        {
            _mainWindow.PowerSupplyTurnOff();
            _powerSupplyTimerManager?.ResetTimer();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _mainWindow.AddScenarioButton.IsEnabled = true;
            FinalizeApplication();
        }

        /// Main functions
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!AreFieldsValid(out var missingFields))
            {
                MessageBox.Show($"Вы не заполнили следующие поля: {string.Join(", ", missingFields)}", "Проверка полей", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            OnStartDisableUI();

            if (IsDirectCountdown())
            {
                SetCountdownModeVisibility(Visibility.Collapsed);
                EnsureSingleRow();
                await StartTimerAsync(false);
            }
            else if (IsReverseCountdown())
            {
                await StartTimerAsync(true);
            }

            OnStopEnableUI();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            FinalizeApplication();
            OnStopEnableUI();
        }
    }
}
