using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AudioDeviceSwitcher
{
    public partial class SettingsWindow : Window
    {
        private MainWindow parentWindow;
        private bool isLoading = true;

        public SettingsWindow(MainWindow parent)
        {
            InitializeComponent();
            parentWindow = parent;

            LoadCurrentSettings();
            LoadHiddenDevices();
            UpdateSupporterTabVisibility();

            isLoading = false;
        }

        /// <summary>
        /// Loads current settings from parent window into UI controls
        /// </summary>
        private void LoadCurrentSettings()
        {
            try
            {
                var settings = parentWindow.currentApplicationSettings;

                if (settings != null)
                {
                    // Load system settings
                    StartInTrayCheckBox.IsChecked = settings.System?.StartInTray ?? false;
                    StartWithWindowsCheckBox.IsChecked = settings.System?.StartWithWindows ?? false;

                    // Load user interaction settings
                    SingleClickCheckBox.IsChecked = settings.UserInteraction?.UseSingleClickToSelectDevice ?? false;

                    // Load display settings
                    ShowHiddenCountCheckBox.IsChecked = settings.Display?.ShowHiddenDeviceCount ?? true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates supporter tab visibility based on donation status
        /// </summary>
        private void UpdateSupporterTabVisibility()
        {
            try
            {
                // Hide supporter tab if user is already a registered donor
                if (parentWindow.IsRegisteredDonor())
                {
                    SupporterTab.Visibility = Visibility.Collapsed;

                    // If supporter tab was selected, switch to settings tab
                    if (MainTabControl.SelectedItem == SupporterTab)
                    {
                        MainTabControl.SelectedItem = SettingsTab;
                    }
                }
                else
                {
                    SupporterTab.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating supporter tab visibility: {ex.Message}");
            }
        }


        /// <summary>
        /// Loads and displays currently hidden devices
        /// </summary>
        private void LoadHiddenDevices()
        {
            try
            {
                HiddenDevicesPanel.Children.Clear();

                var settings = parentWindow.currentApplicationSettings;
                if (settings?.DeviceSettings == null)
                {
                    ShowNoHiddenDevices();
                    return;
                }

                var hiddenDevices = settings.DeviceSettings.Values
                    .Where(d => d.IsHidden)
                    .OrderBy(d => d.DeviceName)
                    .ToList();

                if (hiddenDevices.Count == 0)
                {
                    ShowNoHiddenDevices();
                    return;
                }

                // Update hidden count
                HiddenCountText.Text = $"({hiddenDevices.Count} hidden)";

                // Add each hidden device
                foreach (var device in hiddenDevices)
                {
                    AddHiddenDeviceRow(device);
                }

                // Show the "Show All" button
                ShowAllDevicesButton.Visibility = Visibility.Visible;
                NoHiddenDevicesBorder.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading hidden devices: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows message when no devices are hidden
        /// </summary>
        private void ShowNoHiddenDevices()
        {
            HiddenCountText.Text = "(0 hidden)";
            NoHiddenDevicesBorder.Visibility = Visibility.Visible;
            ShowAllDevicesButton.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Adds a row for a hidden device with show button
        /// </summary>
        private void AddHiddenDeviceRow(DeviceSettings device)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.Margin = new Thickness(0, 0, 0, 8);

            // Device name
            var nameText = new TextBlock
            {
                Text = device.DeviceName,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                ToolTip = device.DeviceName
            };
            Grid.SetColumn(nameText, 0);
            grid.Children.Add(nameText);

            // Show button
            var showButton = new Button
            {
                Content = "Show",
                Width = 50,
                Height = 22,
                FontSize = 10,
                Tag = device.DeviceId,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(34, 197, 94)),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(1)
            };
            showButton.Click += OnShowDeviceClick;
            Grid.SetColumn(showButton, 1);
            grid.Children.Add(showButton);

            HiddenDevicesPanel.Children.Add(grid);
        }

        /// <summary>
        /// Handles individual device show button click
        /// </summary>
        private void OnShowDeviceClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string deviceId)
                {
                    var settings = parentWindow.currentApplicationSettings;
                    if (settings?.DeviceSettings?.ContainsKey(deviceId) == true)
                    {
                        // Unhide the device
                        settings.DeviceSettings[deviceId].IsHidden = false;

                        // Save settings
                        parentWindow.SaveApplicationSettingsToFile();

                        // Refresh the display
                        LoadHiddenDevices();

                        // Refresh main window device list
                        parentWindow.Dispatcher.BeginInvoke(new Action(async () =>
                            await parentWindow.RefreshAudioDeviceListFromSystem()));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing device: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles show all hidden devices button click
        /// </summary>
        private void OnShowAllDevicesClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "This will make all hidden devices visible again. Are you sure?",
                    "Show All Hidden Devices",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var settings = parentWindow.currentApplicationSettings;
                    if (settings?.DeviceSettings != null)
                    {
                        // Unhide all devices
                        foreach (var device in settings.DeviceSettings.Values)
                        {
                            device.IsHidden = false;
                        }

                        // Save settings
                        parentWindow.SaveApplicationSettingsToFile();

                        // Refresh the display
                        LoadHiddenDevices();

                        // Refresh main window device list
                        parentWindow.Dispatcher.BeginInvoke(new Action(async () =>
                            await parentWindow.RefreshAudioDeviceListFromSystem()));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing all devices: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles Save Settings button click
        /// </summary>
        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isLoading) return;

                var settings = parentWindow.currentApplicationSettings;
                if (settings == null) return;

                // Save system settings
                if (settings.System == null)
                    settings.System = new SystemSettings();

                settings.System.StartInTray = StartInTrayCheckBox.IsChecked == true;
                settings.System.StartWithWindows = StartWithWindowsCheckBox.IsChecked == true;

                // Save user interaction settings
                if (settings.UserInteraction == null)
                    settings.UserInteraction = new UserInteractionSettings();

                settings.UserInteraction.UseSingleClickToSelectDevice = SingleClickCheckBox.IsChecked == true;

                // Save display settings
                if (settings.Display == null)
                    settings.Display = new DisplaySettings();

                settings.Display.ShowHiddenDeviceCount = ShowHiddenCountCheckBox.IsChecked == true;

                // Save settings to file and update Windows startup registry
                parentWindow.SaveApplicationSettingsToFile();

                // Update hidden devices display immediately
                parentWindow.UpdateHiddenDevicesDisplay();

                // Close the settings window
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles Reset to Defaults button click
        /// </summary>
        private void OnResetClick(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "This will reset all settings to their default values. Are you sure?",
                "Reset Settings",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                isLoading = true;

                // Reset to default values
                StartInTrayCheckBox.IsChecked = false;
                StartWithWindowsCheckBox.IsChecked = false;
                SingleClickCheckBox.IsChecked = false;
                ShowHiddenCountCheckBox.IsChecked = true;

                isLoading = false;

                MessageBox.Show("Settings reset to defaults. Click 'Save Settings' to apply.",
                    "Settings Reset", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Handles supporter registration button click
        /// </summary>
        private void OnRegisterClick(object sender, RoutedEventArgs e)
        {
            try
            {
                string email = EmailTextBox.Text.Trim();
                string key = KeyTextBox.Text.Trim();

                // Clear previous status
                StatusTextBlock.Text = "";
                StatusTextBlock.Foreground = System.Windows.Media.Brushes.Black;

                // Validate input
                if (string.IsNullOrEmpty(email))
                {
                    ShowStatus("Please enter your email address.", false);
                    EmailTextBox.Focus();
                    return;
                }

                if (string.IsNullOrEmpty(key))
                {
                    ShowStatus("Please enter your supporter key.", false);
                    KeyTextBox.Focus();
                    return;
                }

                // Basic email validation
                if (!email.Contains("@") || !email.Contains("."))
                {
                    ShowStatus("Please enter a valid email address.", false);
                    EmailTextBox.Focus();
                    return;
                }

                // Attempt registration
                bool success = parentWindow.RegisterDonorKey(email, key);

                if (success)
                {
                    ShowStatus("✅ Registration successful! Thank you for your support!", true);

                    // Clear form
                    EmailTextBox.Text = "";
                    KeyTextBox.Text = "";

                    // Hide supporter tab after successful registration
                    UpdateSupporterTabVisibility();

                    // Switch to settings tab
                    MainTabControl.SelectedItem = SettingsTab;
                }
                else
                {
                    ShowStatus("❌ Invalid email or supporter key. Please check your information.", false);
                }
            }
            catch (Exception ex)
            {
                ShowStatus($"❌ Registration error: {ex.Message}", false);
                System.Diagnostics.Debug.WriteLine($"Supporter registration error: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows status message with appropriate styling
        /// </summary>
        private void ShowStatus(string message, bool isSuccess)
        {
            StatusTextBlock.Text = message;
            StatusTextBlock.Foreground = isSuccess
                ? System.Windows.Media.Brushes.Green
                : System.Windows.Media.Brushes.Red;
        }
    }
}