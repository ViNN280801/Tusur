using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TusurUI.Errors;
using TusurUI.Helpers;

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

        private List<int> _cancelledStages;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isRunning;

        private MainWindow _mainWindow;
        private PowerSupplyTimerManager _powerSupplyTimerManager;
        private List<PowerSupplyTimerManager> _timerManagers;

        public ScenariosWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            _mainWindow = mainWindow;
            _powerSupplyTimerManager = new PowerSupplyTimerManager(GetTextBoxByName("TimerHoursTextBox"), GetTextBoxByName("TimerMinutesTextBox"), GetTextBoxByName("TimerSecondsTextBox"), GetProgressBarByName("ProgressBar"), _mainWindow.PowerSupplyTurnOff);
            _timerManagers = new List<PowerSupplyTimerManager>();
            _cancelledStages = new List<int>();
        }
        private void ClearTimerTextBoxes()
        {
            foreach (var panel in ScenarioStackPanel.Children)
            {
                if (panel is StackPanel innerPanel && innerPanel != ScenarioLabelsStackPanel)
                {
                    foreach (var element in innerPanel.Children)
                    {
                        if (element is TextBox textBox &&
                            (textBox.Name == "TimerHoursTextBox" ||
                            textBox.Name == "TimerMinutesTextBox" ||
                            textBox.Name == "TimerSecondsTextBox"))
                        {
                            textBox.Text = string.Empty;
                        }
                    }
                }
            }
        }
        private void SetEnabledTextBoxes(bool isEnabled)
        {
            foreach (var panel in ScenarioStackPanel.Children)
            {
                if (panel is StackPanel innerPanel && innerPanel != ScenarioLabelsStackPanel)
                {
                    foreach (var element in innerPanel.Children)
                    {
                        if (element is TextBox textBox)
                        {
                            textBox.IsEnabled = isEnabled;
                        }
                    }
                }
            }
        }
        private void SetEnabledTimerTextBoxes(bool isEnabled)
        {
            foreach (var panel in ScenarioStackPanel.Children)
            {
                if (panel is StackPanel innerPanel && innerPanel != ScenarioLabelsStackPanel)
                {
                    foreach (var element in innerPanel.Children)
                    {
                        if (element is TextBox textBox &&
                            (textBox.Name == "TimerHoursTextBox" ||
                            textBox.Name == "TimerMinutesTextBox" ||
                            textBox.Name == "TimerSecondsTextBox"))
                        {
                            textBox.IsEnabled = isEnabled;
                        }
                    }
                }
            }
        }
        private void CountdownModeCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            SetEnabledTimerTextBoxes(false);
            ClearTimerTextBoxes();
            SetCountdownModeVisibility(Visibility.Collapsed);
            EnsureSingleRow();
        }
        private void CountdownModeCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            SetEnabledTimerTextBoxes(true);
            SetCountdownModeVisibility(Visibility.Visible);
            ResetProgressBars();
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
                ScenarioStackPanel.Children.RemoveAt(2);
            }
        }

        private void AddRowButton_Click(object sender, RoutedEventArgs e) { AddRow(); }

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
            currentTextBox.ToolTip = string.Format((string)FindResource("CurrentTooltip"), k_nullCurrent, k_MaxCurrent);
            row.Children.Add(currentTextBox);

            Label aLabel = new Label { Content = FindResource("CurrentUnitLabel"), Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(aLabel);

            TextBox timerTextBoxHours = new TextBox { Name = "TimerHoursTextBox", Width = 50, Height = 25, Style = (Style)FindResource("ScenarioTextBoxStyle") };
            timerTextBoxHours.PreviewTextInput += TimerHoursTextBox_PreviewTextInput;
            timerTextBoxHours.ToolTip = string.Format((string)FindResource("HoursTooltip"), k_nullTime, k_MaxHours);
            row.Children.Add(timerTextBoxHours);

            Label hourLabel = new Label { Content = FindResource("HoursLabel"), Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(hourLabel);

            TextBox timerTextBoxMins = new TextBox { Name = "TimerMinutesTextBox", Width = 50, Height = 25, Style = (Style)FindResource("ScenarioTextBoxStyle") };
            timerTextBoxMins.PreviewTextInput += TimerMinutesTextBox_PreviewTextInput;
            timerTextBoxMins.ToolTip = string.Format((string)FindResource("MinutesTooltip"), k_nullTime, k_MaxMinutes);
            row.Children.Add(timerTextBoxMins);

            Label minLabel = new Label { Content = FindResource("MinutesLabel"), Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };
            row.Children.Add(minLabel);

            TextBox timerTextBoxSecs = new TextBox { Name = "TimerSecondsTextBox", Width = 50, Height = 25, Style = (Style)FindResource("ScenarioTextBoxStyle") };
            timerTextBoxSecs.PreviewTextInput += TimerSecondsTextBox_PreviewTextInput;
            timerTextBoxSecs.ToolTip = string.Format((string)FindResource("SecondsTooltip"), k_nullTime, k_MaxSeconds);
            row.Children.Add(timerTextBoxSecs);

            Label secLabel = new Label { Content = FindResource("SecondsLabel"), Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };
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

        public void TranslateUI()
        {
            foreach (var child in ScenarioStackPanel.Children)
            {
                if (child is StackPanel row)
                {
                    foreach (var element in row.Children)
                    {
                        if (element is Label label)
                        {
                            if (label.Content.ToString() == "Величина тока" || label.Content.ToString() == "Current Value")
                                label.Content = FindResource("CurrentLabel");
                            else if (label.Content.ToString() == "Время напыления" || label.Content.ToString() == "Spraying Time")
                                label.Content = FindResource("TimeLabel");
                            else if (label.Content.ToString() == "Прогресс" || label.Content.ToString() == "Progress")
                                label.Content = FindResource("ProgressLabel");
                            else if (label.Content.ToString() == "A")
                                label.Content = FindResource("CurrentUnitLabel");
                            else if (label.Content.ToString() == "час" || label.Content.ToString() == "hr")
                                label.Content = FindResource("HoursLabel");
                            else if (label.Content.ToString() == "мин" || label.Content.ToString() == "min")
                                label.Content = FindResource("MinutesLabel");
                            else if (label.Content.ToString() == "с" || label.Content.ToString() == "sec")
                                label.Content = FindResource("SecondsLabel");
                        }
                        else if (element is TextBox textBox)
                        {
                            if (textBox.Name == "CurrentTextBox")
                                textBox.ToolTip = string.Format((string)FindResource("CurrentTooltip"), k_nullCurrent, k_MaxCurrent);
                            else if (textBox.Name == "TimerHoursTextBox")
                                textBox.ToolTip = string.Format((string)FindResource("HoursTooltip"), k_nullTime, k_MaxHours);
                            else if (textBox.Name == "TimerMinutesTextBox")
                                textBox.ToolTip = string.Format((string)FindResource("MinutesTooltip"), k_nullTime, k_MaxMinutes);
                            else if (textBox.Name == "TimerSecondsTextBox")
                                textBox.ToolTip = string.Format((string)FindResource("SecondsTooltip"), k_nullTime, k_MaxSeconds);
                        }
                        else if (element is CheckBox checkBox)
                        {
                            if (checkBox.Name == "CountdownModeCheckBox")
                                checkBox.Content = FindResource("CountdownModeLabel");
                        }
                        else if (element is Button button)
                        {
                            if (button.Name == "AddRowButton")
                                button.Content = "+";
                            else if (button.Name == "RemoveRowButton")
                                button.Content = "-";
                        }
                    }
                }
            }

            StartButton.Content = FindResource("StartButtonLabel");
            StopButton.Content = FindResource("StopButtonLabel");
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
        private bool IsCurrentFieldValid(TextBox textBox, ref bool allFieldsAreZero, List<string> stageErrors)
        {
            if (!int.TryParse(textBox.Text, out int current) || current <= k_nullCurrent || current > k_MaxCurrent)
            {
                UIHelper.MarkTextBoxAsInvalid(textBox);
                stageErrors.Add(string.Format(ErrorMessages.GetErrorMessage("InvalidCurrentValue"), current, k_nullCurrent, k_MaxCurrent));
                return false;
            }

            if (current != k_nullCurrent)
            {
                allFieldsAreZero = false;
            }

            UIHelper.RestoreTextBoxStyle(textBox);
            return true;
        }
        private bool IsTimerFieldValid(TextBox textBox, ref int totalSeconds, ref bool allFieldsAreZero, List<string> stageErrors)
        {
            int value = int.TryParse(textBox.Text, out int parsedValue) ? parsedValue : k_nullTime;

            if (textBox.Name == "TimerHoursTextBox")
            {
                return IsHoursFieldValid(textBox, value, ref totalSeconds, ref allFieldsAreZero, stageErrors);
            }
            else if (textBox.Name == "TimerMinutesTextBox")
            {
                return IsMinutesFieldValid(textBox, value, ref totalSeconds, ref allFieldsAreZero, stageErrors);
            }
            else if (textBox.Name == "TimerSecondsTextBox")
            {
                return IsSecondsFieldValid(textBox, value, ref totalSeconds, ref allFieldsAreZero, stageErrors);
            }

            return true;
        }
        private bool IsHoursFieldValid(TextBox textBox, int value, ref int totalSeconds, ref bool allFieldsAreZero, List<string> stageErrors)
        {
            if (value < k_nullTime || value >= k_MaxHours)
            {
                UIHelper.MarkTextBoxAsInvalid(textBox);
                stageErrors.Add(string.Format(ErrorMessages.GetErrorMessage("InvalidHoursValue"), value, k_nullTime, k_MaxHours));
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

        private bool IsMinutesFieldValid(TextBox textBox, int value, ref int totalSeconds, ref bool allFieldsAreZero, List<string> stageErrors)
        {
            if (value < k_nullTime || value >= k_MaxMinutes)
            {
                UIHelper.MarkTextBoxAsInvalid(textBox);
                stageErrors.Add(string.Format(ErrorMessages.GetErrorMessage("InvalidMinutesValue"), value, k_nullTime, k_MaxMinutes));
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

        private bool IsSecondsFieldValid(TextBox textBox, int value, ref int totalSeconds, ref bool allFieldsAreZero, List<string> stageErrors)
        {
            if (value < k_nullTime || value >= k_MaxSeconds)
            {
                UIHelper.MarkTextBoxAsInvalid(textBox);
                stageErrors.Add(string.Format(ErrorMessages.GetErrorMessage("InvalidSecondsValue"), value, k_nullTime, k_MaxSeconds));
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
        private bool AreFieldsValid(out List<string> missingFields, out bool allTimeFieldsAreZero, out List<string> errorMessages)
        {
            missingFields = new List<string>();
            errorMessages = new List<string>();
            allTimeFieldsAreZero = true;
            bool allFieldsAreZero = true;
            int stageNumber = 1;

            foreach (var panel in ScenarioStackPanel.Children)
            {
                if (panel is StackPanel innerPanel && innerPanel != ScenarioLabelsStackPanel)
                {
                    int totalSeconds = k_nullTime;
                    bool stageFieldsAreZero = true;
                    List<string> stageErrors = new List<string>();

                    foreach (var element in innerPanel.Children)
                    {
                        if (element is TextBox textBox)
                        {
                            if (textBox.Name == "CurrentTextBox")
                            {
                                if (!IsCurrentFieldValid(textBox, ref allFieldsAreZero, stageErrors))
                                {
                                    missingFields.Add("Величина тока");
                                }
                            }

                            if (IsReverseCountdown())
                            {
                                if (!IsTimerFieldValid(textBox, ref totalSeconds, ref stageFieldsAreZero, stageErrors))
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
                    }

                    if (totalSeconds >= k_SecondsInDay)
                    {
                        stageErrors.Add(ErrorMessages.GetErrorMessage("TotalTimeExceeded"));
                    }

                    if (!stageFieldsAreZero)
                    {
                        allTimeFieldsAreZero = false;
                    }

                    if (stageErrors.Count > 0)
                    {
                        errorMessages.Add(string.Format(ErrorMessages.GetErrorMessage("StageErrors"), stageNumber, string.Join(", ", stageErrors)));
                    }

                    stageNumber++;
                }
            }

            if (allFieldsAreZero)
            {
                errorMessages.Add(ErrorMessages.GetErrorMessage("AllFieldsZero"));
                return false;
            }

            if (IsReverseCountdown())
            {
                if (allTimeFieldsAreZero)
                {
                    errorMessages.Add(ErrorMessages.GetErrorMessage("AllTimeFieldsZero"));
                    return false;
                }
            }

            return missingFields.Count == 0;
        }
        private bool IsDirectCountdown() { return CountdownModeCheckBox.IsChecked == true; }
        private bool IsReverseCountdown() { return CountdownModeCheckBox.IsChecked == false; }
        private async Task StartTimerAsync(bool isReverseCountdown, int stageNumber, StackPanel row, CancellationToken token)
        {
            try
            {
                // Extract the TextBoxes and ProgressBar from the row
                TextBox timerTextBoxHours = row.Children.OfType<TextBox>().First(tb => tb.Name == "TimerHoursTextBox");
                TextBox timerTextBoxMins = row.Children.OfType<TextBox>().First(tb => tb.Name == "TimerMinutesTextBox");
                TextBox timerTextBoxSecs = row.Children.OfType<TextBox>().First(tb => tb.Name == "TimerSecondsTextBox");
                ProgressBar progressBar = row.Children.OfType<ProgressBar>().First(pb => pb.Name == "ProgressBar");

                // Create a new PowerSupplyTimerManager for each row
                var powerSupplyTimerManager = new PowerSupplyTimerManager(timerTextBoxHours, timerTextBoxMins, timerTextBoxSecs, progressBar, _mainWindow.PowerSupplyTurnOff);
                _timerManagers.Add(powerSupplyTimerManager);
                powerSupplyTimerManager.StartCountdown(isReverseCountdown);

                await Task.Run(() =>
                {
                    while (powerSupplyTimerManager.IsRunning)
                    {
                        token.ThrowIfCancellationRequested();
                    }
                }, token);
            }
            catch (OperationCanceledException)
            {
                if (IsReverseCountdown())
                    MessageBox.Show(ErrorMessages.GetErrorMessage("StageCancelledReverse"), ErrorMessages.GetErrorMessage("CancellationTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                else if (IsDirectCountdown())
                    MessageBox.Show(ErrorMessages.GetErrorMessage("StageCancelledDirect"), ErrorMessages.GetErrorMessage("CancellationTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(ErrorMessages.GetErrorMessage("TimerError"), stageNumber, ex.Message), ErrorMessages.GetErrorMessage("TimerErrorTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
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
            RemoveRowButton.IsEnabled = false;
        }
        private void ResetProgressBars()
        {
            foreach (var child in ScenarioStackPanel.Children)
            {
                if (child is StackPanel row && row != ScenarioLabelsStackPanel)
                {
                    foreach (var element in row.Children)
                    {
                        if (element is ProgressBar progressBar)
                        {
                            progressBar.Minimum = 0;
                            progressBar.Maximum = 1;
                            progressBar.Value = 0;
                        }
                    }
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
            string errMessage = ErrorMessages.Compose(
                ErrorMessages.GetErrorMessage("InternalError"),
                string.Format(ErrorMessages.GetErrorMessage("TextBoxNotFound"), name)
            );
            throw new Exception(errMessage);
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
            string errMessage = ErrorMessages.Compose(
                ErrorMessages.GetErrorMessage("InternalError"),
                string.Format(ErrorMessages.GetErrorMessage("ProgressBarNotFound"), name)
            );
            throw new Exception(errMessage);
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
        private void OnStart()
        {
            OnStartDisableUI();
        }
        private void OnStop()
        {
            OnStopEnableUI();
        }
        private void FinalizeApplication()
        {
            _mainWindow.AddScenarioButton.IsEnabled = true;
            if (_mainWindow._powerSupplyManager.IsConnected())
                _mainWindow.PowerSupplyTurnOff();
            _powerSupplyTimerManager?.ResetTimer();

            foreach (var manager in _timerManagers)
                manager.ResetTimer();
            ResetProgressBars();
            _timerManagers.Clear();
        }
        private void Window_Closed(object sender, EventArgs e) { FinalizeApplication(); }

        /// Main functions
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            ResetProgressBars();
            if (!AreFieldsValid(out var missingFields, out bool allTimeFieldsAreZero, out var errorMessages))
            {
                string combinedErrors = string.Join("\n", errorMessages);
                MessageBox.Show(string.Format(ErrorMessages.GetErrorMessage("InvalidFields"), combinedErrors), ErrorMessages.GetErrorMessage("ValidationTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;
            _isRunning = true;
            _cancelledStages.Clear();

            OnStart();

            int stageNumber = 1;
            foreach (StackPanel row in ScenarioStackPanel.Children)
            {
                if (row.Name != "ScenarioLabelsStackPanel")
                {
                    if (!_isRunning)
                        break;

                    try
                    {
                        if (IsDirectCountdown())
                        {
                            SetCountdownModeVisibility(Visibility.Collapsed);
                            EnsureSingleRow();
                            await StartTimerAsync(false, stageNumber, row, token);
                            ResetProgressBars();
                        }
                        else if (IsReverseCountdown())
                        {
                            await StartTimerAsync(true, stageNumber, row, token);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        _cancelledStages.Add(stageNumber);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Стадия {stageNumber}: {ex.Message}", "Ошибка таймера", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    stageNumber++;
                }
            }

            OnStop();
            _isRunning = false;
        }
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _isRunning = false;
            _cancellationTokenSource?.Cancel();
            ClearTimerTextBoxes();
            FinalizeApplication();
            OnStopEnableUI();
        }
    }
}
