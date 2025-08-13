using AudioSwitcher.AudioApi;
using AudioSwitcher.AudioApi.CoreAudio;
using AudioSwitcher.AudioApi.Observables;
using System.ComponentModel;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using WpfButton = System.Windows.Controls.Button;
using WpfTextBox = System.Windows.Controls.TextBox;
using WpfWindow = System.Windows.Window;


namespace AudioDeviceSwitcher
{
    /// <summary>
    /// Settings data structure that will be saved to and loaded from settings.json
    /// Contains all user preferences and application state that should persist
    /// </summary>
    public class ApplicationSettings
    {
        /// <summary>
        /// Window position and size settings
        /// </summary>
        public WindowSettings Window { get; set; } = new WindowSettings();

        /// <summary>
        /// User interaction preferences and behavior settings
        /// </summary>
        public UserInteractionSettings UserInteraction { get; set; } = new UserInteractionSettings();

        /// <summary>
        /// Display and UI preferences
        /// </summary>
        public DisplaySettings Display { get; set; } = new DisplaySettings();
        /// <summary>
        /// Display and UI preferences
        /// </summary>
    

        /// <summary>
        /// System integration and startup preferences
        /// </summary>
        public SystemSettings System { get; set; } = new SystemSettings();

        /// <summary>
        /// Device-specific settings indexed by device ID
        /// </summary>
        public Dictionary<string, DeviceSettings> DeviceSettings { get; set; } = new Dictionary<string, DeviceSettings>();
    }





    /// <summary>
    /// Window-specific settings like position, size, and state
    /// </summary>
    public class WindowSettings
    {
        /// <summary>
        /// X coordinate of window position on screen
        /// </summary>
        public double Left { get; set; } = 100;

        /// <summary>
        /// Y coordinate of window position on screen
        /// </summary>
        public double Top { get; set; } = 100;

        /// <summary>
        /// Width of the window
        /// </summary>
        public double Width { get; set; } = 800;

        /// <summary>
        /// Height of the window
        /// </summary>
        public double Height { get; set; } = 500;

        /// <summary>
        /// Window state (Normal, Minimized, Maximized)
        /// </summary>
        public WindowState WindowState { get; set; } = WindowState.Normal;

        /// <summary>
        /// Width of the Device Name column in the list
        /// </summary>
        public double DeviceNameColumnWidth { get; set; } = 350;

        /// <summary>
        /// Width of the Status column in the list
        /// </summary>
        public double StatusColumnWidth { get; set; } = 80;

        /// <summary>
        /// Width of the Hotkey column in the list
        /// </summary>
        public double HotkeyColumnWidth { get; set; } = 120;

        /// <summary>
        /// Width of the Remarks column in the list
        /// </summary>
        public double RemarksColumnWidth { get; set; } = 200;
    }

    /// <summary>
    /// User interaction settings and preferences
    /// </summary>
    public class UserInteractionSettings
    {
        /// <summary>
        /// Click mode for device selection: true for single-click, false for double-click
        /// </summary>
        public bool UseSingleClickToSelectDevice { get; set; } = false;
    }

    /// <summary>
    /// Display settings for the user interface
    /// </summary>
    public class DisplaySettings
    {
        /// <summary>
        /// Whether to show the hidden devices count line in status area
        /// </summary>
        public bool ShowHiddenDeviceCount { get; set; } = true;
    }

    /// <summary>
    /// System integration and startup settings
    /// </summary>
    public class SystemSettings
    {
        /// <summary>
        /// Whether to start the application minimized to system tray
        /// </summary>
        public bool StartInTray { get; set; } = false;

        /// <summary>
        /// Whether to start the application automatically with Windows
        /// </summary>
        public bool StartWithWindows { get; set; } = false;
    }
    
    /// <summary>
    /// Device-specific settings that persist across sessions
    /// Contains user preferences for individual audio devices
    /// </summary>
    public class DeviceSettings
    {
        /// <summary>
        /// Unique identifier for the audio device
        /// </summary>
        public string DeviceId { get; set; }

        /// <summary>
        /// Display name of the device for reference
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// Whether this device should be hidden from the device list
        /// </summary>
        public bool IsHidden { get; set; } = false;

        /// <summary>
        /// Custom user remark/note for this device
        /// </summary>
        public string UserRemark { get; set; } = "";

        /// <summary>
        /// Global hotkey settings for quick device switching
        /// </summary>
        public HotkeySettings Hotkey { get; set; } = new HotkeySettings();

        /// <summary>
        /// Simplified unique identifier for external program access (D1, D2, etc.)
        /// </summary>
        public string SimplifiedId { get; set; } = "";
        /// <summary>
        /// Custom user-selected icon for this device (overrides auto-detection)
        /// </summary>
        public string CustomIcon { get; set; } = "";
    }

    /// <summary>
    /// Hotkey combination settings for device switching
    /// </summary>
    public class HotkeySettings
    {
        /// <summary>
        /// Whether a hotkey is assigned to this device
        /// </summary>
        public bool IsEnabled { get; set; } = false;

        /// <summary>
        /// Modifier keys (Ctrl, Alt, Shift, Win)
        /// </summary>
        public List<string> Modifiers { get; set; } = new List<string>();

        /// <summary>
        /// Main key for the hotkey combination
        /// </summary>
        public string Key { get; set; } = "";

        /// <summary>
        /// Virtual key code for Windows API registration
        /// </summary>
        public uint VirtualKeyCode { get; set; } = 0;

        /// <summary>
        /// Modifier flags for Windows API registration
        /// </summary>
        public uint ModifierFlags { get; set; } = 0;
    }

    public partial class MainWindow : Window, IDisposable, INotifyPropertyChanged
    {
        #region Windows API for Global Hotkeys

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;

        // Modifier key constants
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        #endregion

        // Registry key path for sharing current device info with other programs
        private const string REGISTRY_KEY_PATH = @"SOFTWARE\AudioDeviceSwitcher";
        private const string REGISTRY_VALUE_NAME = "CurrentAudioDevice";
        private const string REGISTRY_SWITCH_VALUE_NAME = "SwitchToAudioDevice";
        private const string REGISTRY_CURRENT_ID_VALUE_NAME = "CurrentAudioDeviceID";

        // Core audio system controller for managing audio devices
        private readonly CoreAudioController audioSystemController;

        // Timer for periodic device list updates as fallback mechanism
        private readonly DispatcherTimer deviceListRefreshTimer;
        // Timer for monitoring registry switch commands
        private readonly DispatcherTimer registrySwitchMonitorTimer;

        // Donation system constants and fields
        private const string FIRST_RUN_DATE_KEY = "FirstRunDate";

        // Email-based donor key system - dual registry storage for validation
        private const string DONOR_EMAIL_PRIMARY = "UserEmail";
        private const string DONOR_KEY_PRIMARY = "UserKey";
        private const string DONOR_EMAIL_SECONDARY = "SystemEmail";
        private const string DONOR_KEY_SECONDARY = "SystemKey";
        private const string DONOR_KEY_ALT_PATH = @"OFTWARE\YourApp\Validation";

        // Secret phrase for email-based key generation
        private const string SECRET_PHRASE_1 = "ASeccretPhrase";

        private readonly DispatcherTimer donationReminderTimer;
        private bool isDonationDialogShown = false;


        // Flag to prevent multiple dispose calls
        private bool isDisposed = false;

        // Settings file path - located next to executable
        private readonly string settingsFilePath;

        // Current application settings loaded from file - made public for DeviceViewModel access
        public ApplicationSettings currentApplicationSettings;

        // Global hotkey management
        private readonly Dictionary<int, string> registeredHotkeys = new Dictionary<int, string>();
        // System tray icon and menu
        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenuStrip trayContextMenu;

        private int nextHotkeyId = 1;

        // Hidden devices count for status display
        private int hiddenDeviceCount = 0;

        // Collection of audio devices displayed in the UI - bound to ListView
        public ObservableCollection<DeviceViewModel> AudioDeviceList { get; } = new ObservableCollection<DeviceViewModel>();

        // Status message shown in the status bar at bottom of window
        private string currentStatusMessage = "Ready";
        public string CurrentStatusMessage
        {
            get => currentStatusMessage;
            set
            {
                if (currentStatusMessage != value)
                {
                    currentStatusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        // Hidden devices count message for status display
        public string HiddenDevicesMessage { get; set; } = "";

        // Visibility of hidden devices count line
        public Visibility HiddenDevicesMessageVisibility { get; set; } = Visibility.Visible;

        public MainWindow()
        {
            InitializeComponent();

            // Set this window as the data context for XAML binding
            DataContext = this;

            // Initialize settings file path (same directory as executable)
            settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

            // Load settings and apply them to window before showing
            LoadApplicationSettingsFromFile();
            ApplyWindowSettingsToCurrentWindow();

            // Initialize the core audio system controller
            audioSystemController = new CoreAudioController();

            // Set up periodic refresh timer (every 5 seconds as fallback)
            // This ensures UI stays updated even if event notifications are missed
            deviceListRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            deviceListRefreshTimer.Tick += OnDeviceListRefreshTimer_Tick;


            // Set up registry switch monitor timer (every 500ms for responsive external control)
            registrySwitchMonitorTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            registrySwitchMonitorTimer.Tick += OnRegistrySwitchMonitor_Tick;

            // Set up donation reminder timer (will be started after checking 30-day usage)
            donationReminderTimer = new DispatcherTimer();
            donationReminderTimer.Tick += OnDonationReminder_Tick;

            // Subscribe to real-time audio device change events for immediate updates
            // This provides faster response than relying solely on timer
            audioSystemController.AudioDeviceChanged.Subscribe(OnAudioSystemDeviceChanged);

            // Apply column widths after window is fully loaded
            this.Loaded += OnMainWindow_Loaded;

            // Set up global hotkey message handling
            this.SourceInitialized += OnMainWindow_SourceInitialized;

            // Load the initial list of audio devices
            LoadInitialAudioDeviceList();
            // Initialize system tray icon
            InitializeSystemTrayIcon();
            // Apply system settings (Windows startup registry)
            UpdateWindowsStartupRegistry();
        }

        /// <summary>
        /// Handles window source initialized event - sets up global hotkey message handling
        /// </summary>
        private void OnMainWindow_SourceInitialized(object sender, EventArgs e)
        {
            // Add hook for global hotkey messages
            var source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            source?.AddHook(HandleGlobalHotkeyMessages);

            // Register all saved hotkeys
            RegisterAllHotkeys();
        }

        /// <summary>
        /// Handles window loaded event - applies column widths and sets up column resize monitoring
        /// </summary>
        private void OnMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyColumnWidthsFromSettings();
            SetupColumnResizeMonitoring();
            UpdateHiddenDevicesDisplay();
        }

        /// <summary>
        /// Loads the initial list of audio devices when the application starts
        /// Shows loading status to user and starts the refresh timer
        /// </summary>
        private async void LoadInitialAudioDeviceList()
        {
            // Update status to show we're loading devices
            CurrentStatusMessage = "Loading available audio devices...";

            // Refresh the device list from the system
            await RefreshAudioDeviceListFromSystem();

            // Update status with instructions for user based on click mode
            string clickInstruction = currentApplicationSettings?.UserInteraction?.UseSingleClickToSelectDevice == true
                ? "Single-click" : "Double-click";
            CurrentStatusMessage = $"Ready - {clickInstruction} any device to set it as default";

            // Start the fallback refresh timer
            deviceListRefreshTimer.Start();
            // Start the registry switch monitor timer
            registrySwitchMonitorTimer.Start();
            // Initialize donation system (check if user has been using for 30+ days)
            InitializeDonationSystem();
        }

        /// <summary>
        /// Handles real-time audio device change events from the system
        /// Triggers immediate UI update when devices are added/removed/changed
        /// </summary>
        private void OnAudioSystemDeviceChanged(DeviceChangedArgs deviceChangeArgs)
        {
            // Update UI on main thread when system audio devices change
            // This provides immediate feedback instead of waiting for timer
            Dispatcher.BeginInvoke(new Action(async () => await RefreshAudioDeviceListFromSystem()));
        }

        /// <summary>
        /// Periodic timer tick handler - serves as fallback refresh mechanism
        /// Ensures UI stays synchronized even if event notifications are missed
        /// </summary>
        private void OnDeviceListRefreshTimer_Tick(object sender, EventArgs e)
        {
            // Trigger device list refresh as fallback safety measure
            _ = RefreshAudioDeviceListFromSystem();
        }

        /// <summary>
        /// Refreshes the audio device list from the system and updates the UI
        /// Uses efficient differential updates to minimize UI flicker
        /// </summary>
        public async Task RefreshAudioDeviceListFromSystem()
        {
            try
            {
                // Get current active playback devices from system on background thread
                // This prevents UI freezing during device enumeration
                var activePlaybackDevices = await Task.Run(() =>
                    audioSystemController.GetPlaybackDevices(DeviceState.Active).ToList());

                // Update UI on main thread with thread-safe dispatcher invoke
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Create snapshot of current UI device list for comparison
                    var currentDisplayedDevices = AudioDeviceList.ToList();

                    // Create set of device IDs from system for efficient lookup
                    var systemDeviceIds = activePlaybackDevices.Select(device => device.Id).ToHashSet();

                    // Remove devices from UI that no longer exist in system
                    // Loop backwards to safely remove items during iteration
                    for (int deviceIndex = currentDisplayedDevices.Count - 1; deviceIndex >= 0; deviceIndex--)
                    {
                        var displayedDevice = currentDisplayedDevices[deviceIndex].AudioDevice;

                        // Remove device if it's null or no longer exists in system
                        if (displayedDevice == null || !systemDeviceIds.Contains(displayedDevice.Id))
                        {
                            AudioDeviceList.RemoveAt(deviceIndex);
                        }
                    }

                    // Add new devices or update existing ones
                    foreach (var systemDevice in activePlaybackDevices)
                    {
                        // Always save device info regardless of visibility (for remarks and hotkeys)
                        SaveDeviceInfoToSettings(systemDevice);

                        // Check if device should be hidden
                        if (IsDeviceHidden(systemDevice.Id.ToString()))
                        {
                            continue; // Skip hidden devices
                        }

                        // Look for existing device in UI list
                        var existingDeviceViewModel = AudioDeviceList.FirstOrDefault(
                            displayedDevice => displayedDevice.AudioDevice?.Id == systemDevice.Id);

                        if (existingDeviceViewModel != null)
                        {
                            // Update existing device with latest information
                            existingDeviceViewModel.UpdateFromAudioDevice(systemDevice);
                        }
                        else
                        {
                            // Add new device to UI list
                            AudioDeviceList.Add(new DeviceViewModel(systemDevice, this));
                        }
                    }

                    // Update registry with current default device
                    UpdateCurrentDeviceInRegistry(activePlaybackDevices);

                    // Update hidden devices count and display
                    UpdateHiddenDeviceCount(activePlaybackDevices);
                    UpdateHiddenDevicesDisplay();

                    // Update tray icon tooltip with current device
                    UpdateTrayIconTooltip();
                });
            }
            catch (Exception refreshException)
            {
                // Handle refresh errors gracefully - update status and log for debugging
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentStatusMessage = $"Error refreshing device list: {refreshException.Message}";
                    System.Diagnostics.Debug.WriteLine($"RefreshAudioDeviceListFromSystem error: {refreshException}");
                });
            }
        }

        /// <summary>
        /// Handles double-click events on audio devices in the list
        /// Sets the clicked device as the system's default audio device
        /// Only active when double-click mode is enabled in settings
        /// </summary>
        private async void OnAudioDevicesPanel_MouseDoubleClick(object sender, MouseButtonEventArgs mouseClickArgs)
        {
            // Only process double-click if double-click mode is enabled
            if (currentApplicationSettings?.UserInteraction?.UseSingleClickToSelectDevice == false)
            {
                await ProcessDeviceSelection();
            }
        }

        /// <summary>
        /// Handles single-click events on audio devices in the list
        /// Sets the clicked device as the system's default audio device
        /// Only active when single-click mode is enabled in settings
        /// </summary>
        private async void OnAudioDevicesPanel_MouseSingleClick(object sender, MouseButtonEventArgs mouseClickArgs)
        {
            // Only process single-click if single-click mode is enabled
            if (currentApplicationSettings?.UserInteraction?.UseSingleClickToSelectDevice == true)
            {
                await ProcessDeviceSelection();
            }
        }

        /// <summary>
        /// Handles right-click events on audio devices in the list
        /// Shows context menu with device options
        /// </summary>
        private void OnAudioDevicesPanel_MouseRightClick(object sender, MouseButtonEventArgs mouseClickArgs)
        {
            // Get the selected device from the ListView
            if (AudioDevicesPanel.SelectedItem is DeviceViewModel selectedDeviceViewModel)
            {
                // Get mouse position for context menu placement
                var position = mouseClickArgs.GetPosition(AudioDevicesPanel);
                ShowDeviceContextMenu(selectedDeviceViewModel, position);
            }
        }

        /// <summary>
        /// Common method to handle device selection regardless of click type
        /// Sets the selected device as the system's default audio device
        /// </summary>
        private async Task ProcessDeviceSelection()
        {
            // Get the selected device from the ListView
            if (AudioDevicesPanel.SelectedItem is DeviceViewModel selectedDeviceViewModel &&
                selectedDeviceViewModel.AudioDevice != null)
            {
                try
                {
                    // Show user that we're processing their request
                    CurrentStatusMessage = $"Setting '{selectedDeviceViewModel.DeviceName}' as default audio device...";

                    // Temporarily disable the device list to prevent multiple simultaneous operations
                    AudioDevicesPanel.IsEnabled = false;

                    // Perform the device switch operation on background thread to avoid UI freezing
                    await Task.Run(async () =>
                    {
                        try
                        {
                            // Try async method first (preferred for newer AudioSwitcher versions)
                            await selectedDeviceViewModel.AudioDevice.SetAsDefaultAsync();
                        }
                        catch (MissingMethodException)
                        {
                            // Fallback to synchronous method for older AudioSwitcher versions
                            selectedDeviceViewModel.AudioDevice.SetAsDefault();
                        }
                    });

                    // Update status with success message
                    CurrentStatusMessage = $"'{selectedDeviceViewModel.DeviceName}' is now the default audio device";

                    // Refresh the device list to show updated default device status
                    await RefreshAudioDeviceListFromSystem();
                }
                catch (Exception deviceSwitchException)
                {
                    // Handle errors gracefully - show user-friendly message
                    CurrentStatusMessage = $"Failed to set device: {deviceSwitchException.Message}";

                    // Show detailed error dialog for user troubleshooting
                    MessageBox.Show($"Unable to set '{selectedDeviceViewModel.DeviceName}' as default device:\n\n{deviceSwitchException.Message}",
                        "Audio Device Switch Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                finally
                {
                    // Always re-enable the device list, even if operation failed
                    AudioDevicesPanel.IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// Handles manual refresh icon click - allows user to force device list update
        /// </summary>
        private void OnManualRefreshIcon_Click(object sender, RoutedEventArgs buttonClickArgs)
        {
            // Trigger immediate device list refresh when user clicks refresh icon
            _ = RefreshAudioDeviceListFromSystem();
        }

        /// <summary>
        /// Handles About link click - shows the About dialog
        /// </summary>
        private void OnAboutClick(object sender, RoutedEventArgs e)
        {
            var aboutWindow = new AboutWindow
            {
                Owner = this
            };
            aboutWindow.ShowDialog();
        }

        /// <summary>
        /// Handles window closing event - hide to tray instead of closing, unless shutting down
        /// </summary>
        protected override void OnClosing(CancelEventArgs windowClosingArgs)
        {
            // If application is shutting down, allow normal close
            if (Application.Current.ShutdownMode == ShutdownMode.OnExplicitShutdown ||
                Application.Current.MainWindow == null)
            {
                // Perform normal shutdown cleanup
                PerformShutdownCleanup();
                base.OnClosing(windowClosingArgs);
                return;
            }

            // Otherwise, cancel close and hide to tray
            windowClosingArgs.Cancel = true;
            HideMainWindow();
        }

        /// <summary>
        /// Performs cleanup operations during application shutdown
        /// </summary>
        private void PerformShutdownCleanup()
        {
            // Unregister all global hotkeys before closing
            UnregisterAllHotkeys();

            // Save current window settings before closing
            SaveCurrentWindowSettingsToFile();

            // Final save of column widths (in case monitoring missed any changes)
            SaveCurrentColumnWidthsToSettings();
            SaveApplicationSettingsToFile();

            // Clean up resources before window closes
            Dispose();
        }
        /// <summary>
        /// Disposes of system resources to prevent memory leaks
        /// Implements IDisposable pattern for proper resource management
        /// </summary>
        public void Dispose()
        {
            if (!isDisposed)
            {
                // Stop and dispose of the refresh timer
                deviceListRefreshTimer?.Stop();
                // Stop and dispose of the registry monitor timer
                registrySwitchMonitorTimer?.Stop();

                // Stop and dispose of the donation reminder timer
                donationReminderTimer?.Stop();


                // Dispose of the audio system controller
                audioSystemController?.Dispose();
                // Dispose of the audio system controller
                audioSystemController?.Dispose();

                // Clean up tray icon
                if (trayIcon != null)
                {
                    trayIcon.Visible = false;
                    trayIcon.Dispose();
                }

                // Clean up tray context menu
                trayContextMenu?.Dispose();

                // Mark as disposed to prevent multiple disposal
                isDisposed = true;
            }
        }

        #region Settings Management

        /// <summary>
        /// Loads application settings from settings.json file
        /// Creates default settings if file doesn't exist or is corrupted
        /// </summary>
        private void LoadApplicationSettingsFromFile()
        {
            try
            {
                // Check if settings file exists
                if (File.Exists(settingsFilePath))
                {
                    // Read and parse JSON settings file
                    string settingsJsonContent = File.ReadAllText(settingsFilePath);

                    if (!string.IsNullOrWhiteSpace(settingsJsonContent))
                    {
                        // Deserialize JSON to settings object
                        currentApplicationSettings = JsonConvert.DeserializeObject<ApplicationSettings>(settingsJsonContent);

                        // Validate loaded settings for integrity
                        ValidateAndFixLoadedSettings();

                        CurrentStatusMessage = "Settings loaded successfully";
                        return;
                    }
                }

                // File doesn't exist or is empty - create default settings
                CreateDefaultSettings();
                CurrentStatusMessage = "Default settings created";
            }
            catch (Exception settingsLoadException)
            {
                // Handle any errors during settings loading (corrupted file, permission issues, etc.)
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {settingsLoadException.Message}");

                // Create default settings as fallback
                CreateDefaultSettings();
                CurrentStatusMessage = "Settings reset to defaults due to loading error";
            }
        }

        /// <summary>
        /// Creates default application settings when no valid settings file exists
        /// </summary>
        private void CreateDefaultSettings()
        {
            currentApplicationSettings = new ApplicationSettings();

            // Save default settings to file immediately
            SaveApplicationSettingsToFile();
        }

        /// <summary>
        /// Validates loaded settings and fixes any invalid values to prevent crashes
        /// Ensures window position is within screen bounds and values are reasonable
        /// </summary>
        private void ValidateAndFixLoadedSettings()
        {
            // Ensure settings object exists
            if (currentApplicationSettings == null)
            {
                currentApplicationSettings = new ApplicationSettings();
                return;
            }

            // Ensure window settings exist
            if (currentApplicationSettings.Window == null)
            {
                currentApplicationSettings.Window = new WindowSettings();
            }

            // Ensure user interaction settings exist
            if (currentApplicationSettings.UserInteraction == null)
            {
                currentApplicationSettings.UserInteraction = new UserInteractionSettings();
            }

            // Ensure display settings exist
            if (currentApplicationSettings.Display == null)
            {
                currentApplicationSettings.Display = new DisplaySettings();
            }

            // Ensure display settings exist
            if (currentApplicationSettings.Display == null)
            {
                currentApplicationSettings.Display = new DisplaySettings();
            }

            // Ensure system settings exist
            if (currentApplicationSettings.System == null)
            {
                currentApplicationSettings.System = new SystemSettings();
            }

            // Ensure device settings exist
            if (currentApplicationSettings.DeviceSettings == null)
            {
                currentApplicationSettings.DeviceSettings = new Dictionary<string, DeviceSettings>();
            }

            var windowSettings = currentApplicationSettings.Window;

            // Validate and fix window size (must be positive and reasonable)
            if (windowSettings.Width < 300 || windowSettings.Width > 3000)
                windowSettings.Width = 800;

            if (windowSettings.Height < 200 || windowSettings.Height > 2000)
                windowSettings.Height = 500;

            // Validate window position (must be within screen bounds)
            var screenWidth = SystemParameters.PrimaryScreenWidth;
            var screenHeight = SystemParameters.PrimaryScreenHeight;

            if (windowSettings.Left < 0 || windowSettings.Left > screenWidth - 100)
                windowSettings.Left = 100;

            if (windowSettings.Top < 0 || windowSettings.Top > screenHeight - 100)
                windowSettings.Top = 100;

            // Validate window state
            if (!Enum.IsDefined(typeof(WindowState), windowSettings.WindowState))
                windowSettings.WindowState = WindowState.Normal;

            
            // Validate column widths - allow very small widths, only prevent extreme values
            if (windowSettings.DeviceNameColumnWidth < 10 || windowSettings.DeviceNameColumnWidth > 2000)
                windowSettings.DeviceNameColumnWidth = 350;

            if (windowSettings.StatusColumnWidth < 10 || windowSettings.StatusColumnWidth > 2000)
                windowSettings.StatusColumnWidth = 80;

            if (windowSettings.HotkeyColumnWidth < 10 || windowSettings.HotkeyColumnWidth > 2000)
                windowSettings.HotkeyColumnWidth = 120;

            if (windowSettings.RemarksColumnWidth < 10 || windowSettings.RemarksColumnWidth > 2000)
                windowSettings.RemarksColumnWidth = 200;
        }

        /// <summary>
        /// Applies loaded window settings to the current window
        /// Called during application startup to restore previous window state
        /// </summary>
        private void ApplyWindowSettingsToCurrentWindow()
        {
            if (currentApplicationSettings?.Window != null)
            {
                var windowSettings = currentApplicationSettings.Window;

                // Apply position and size
                this.Left = windowSettings.Left;
                this.Top = windowSettings.Top;
                this.Width = windowSettings.Width;
                this.Height = windowSettings.Height;
                this.WindowState = windowSettings.WindowState;
            }
        }

        /// <summary>
        /// Applies loaded column width settings to the ListView columns
        /// Called after window is fully loaded to restore previous column sizes
        /// </summary>
        private void ApplyColumnWidthsFromSettings()
        {
            if (currentApplicationSettings?.Window != null)
            {
                // Delay the application to ensure GridView is fully rendered
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (AudioDevicesPanel.View is System.Windows.Controls.GridView gridView)
                    {
                        // Apply widths and force them to stick
                        if (gridView.Columns.Count > 0)
                        {
                            var targetWidth = currentApplicationSettings.Window.DeviceNameColumnWidth;
                            gridView.Columns[0].Width = targetWidth;
                            // Force the width by setting it twice with a slight delay
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                gridView.Columns[0].Width = targetWidth;
                            }), System.Windows.Threading.DispatcherPriority.Render);
                            System.Diagnostics.Debug.WriteLine($"Applied Device Name column width: {targetWidth}");
                        }

                        if (gridView.Columns.Count > 1)
                        {
                            var targetWidth = currentApplicationSettings.Window.StatusColumnWidth;
                            gridView.Columns[1].Width = targetWidth;
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                gridView.Columns[1].Width = targetWidth;
                            }), System.Windows.Threading.DispatcherPriority.Render);
                            System.Diagnostics.Debug.WriteLine($"Applied Status column width: {targetWidth}");
                        }

                        if (gridView.Columns.Count > 2)
                        {
                            var targetWidth = currentApplicationSettings.Window.HotkeyColumnWidth;
                            gridView.Columns[2].Width = targetWidth;
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                gridView.Columns[2].Width = targetWidth;
                            }), System.Windows.Threading.DispatcherPriority.Render);
                            System.Diagnostics.Debug.WriteLine($"Applied Hotkey column width: {targetWidth}");
                        }

                        if (gridView.Columns.Count > 3)
                        {
                            var targetWidth = currentApplicationSettings.Window.RemarksColumnWidth;
                            gridView.Columns[3].Width = targetWidth;
                            this.Dispatcher.BeginInvoke(new Action(() =>
                            {
                                gridView.Columns[3].Width = targetWidth;
                            }), System.Windows.Threading.DispatcherPriority.Render);
                            System.Diagnostics.Debug.WriteLine($"Applied Remarks column width: {targetWidth}");
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        /// <summary>
        /// Sets up monitoring for column resize events to automatically save changes
        /// </summary>
        private void SetupColumnResizeMonitoring()
        {
            if (AudioDevicesPanel.View is System.Windows.Controls.GridView gridView)
            {
                // Monitor each column for width changes
                foreach (var column in gridView.Columns)
                {
                    // Subscribe to width change property
                    var descriptor = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(
                        System.Windows.Controls.GridViewColumn.WidthProperty,
                        typeof(System.Windows.Controls.GridViewColumn));

                    descriptor?.AddValueChanged(column, OnColumnWidthChanged);
                }
            }
        }

        /// <summary>
        /// Handles column width changes and saves them to settings immediately
        /// </summary>
        private void OnColumnWidthChanged(object sender, EventArgs e)
        {
            // Save column widths immediately when user resizes them
            SaveCurrentColumnWidthsToSettings();

            // Also save to file so changes persist across sessions
            SaveApplicationSettingsToFile();
        }

        /// <summary>
        /// Saves current window position and size to settings before application closes
        /// </summary>
        private void SaveCurrentWindowSettingsToFile()
        {
            try
            {
                // Update settings with current window state
                if (currentApplicationSettings?.Window != null)
                {
                    // Only save position/size if window is in normal state
                    // (don't save maximized/minimized dimensions)
                    if (this.WindowState == WindowState.Normal)
                    {
                        currentApplicationSettings.Window.Left = this.Left;
                        currentApplicationSettings.Window.Top = this.Top;
                        currentApplicationSettings.Window.Width = this.Width;
                        currentApplicationSettings.Window.Height = this.Height;
                    }

                    // Always save window state
                    currentApplicationSettings.Window.WindowState = this.WindowState;
                }

                // Save updated settings to file
                SaveApplicationSettingsToFile();
            }
            catch (Exception settingsSaveException)
            {
                // Log error but don't crash application during shutdown
                System.Diagnostics.Debug.WriteLine($"Error saving window settings: {settingsSaveException.Message}");
            }
        }

        /// <summary>
        /// Saves current column widths to settings 
        /// Called when columns are resized or before application closes
        /// </summary>
        private void SaveCurrentColumnWidthsToSettings()
        {
            try
            {
                if (currentApplicationSettings?.Window != null && AudioDevicesPanel.View is System.Windows.Controls.GridView gridView)
                {
                    // Save current column widths using ActualWidth for accurate values
                    if (gridView.Columns.Count > 0)
                    {
                        var newDeviceNameWidth = gridView.Columns[0].ActualWidth;

                        // Only update if width is reasonable and different from current
                        if (newDeviceNameWidth > 10 && newDeviceNameWidth < 2000 &&
                            Math.Abs(newDeviceNameWidth - currentApplicationSettings.Window.DeviceNameColumnWidth) > 1)
                        {
                            currentApplicationSettings.Window.DeviceNameColumnWidth = newDeviceNameWidth;
                            System.Diagnostics.Debug.WriteLine($"Saved Device Name column width: {newDeviceNameWidth}");
                        }
                    }

                    if (gridView.Columns.Count > 2)
                    {
                        var newHotkeyWidth = gridView.Columns[2].ActualWidth;

                        // Only update if width is reasonable and different from current
                        if (newHotkeyWidth > 10 && newHotkeyWidth < 500 &&
                            Math.Abs(newHotkeyWidth - currentApplicationSettings.Window.HotkeyColumnWidth) > 1)
                        {
                            currentApplicationSettings.Window.HotkeyColumnWidth = newHotkeyWidth;
                            System.Diagnostics.Debug.WriteLine($"Saved Hotkey column width: {newHotkeyWidth}");
                        }
                    }

                    if (gridView.Columns.Count > 3)
                    {
                        var newRemarksWidth = gridView.Columns[3].ActualWidth;

                        // Only update if width is reasonable and different from current
                        if (newRemarksWidth > 10 && newRemarksWidth < 800 &&
                            Math.Abs(newRemarksWidth - currentApplicationSettings.Window.RemarksColumnWidth) > 1)
                        {
                            currentApplicationSettings.Window.RemarksColumnWidth = newRemarksWidth;
                            System.Diagnostics.Debug.WriteLine($"Saved Remarks column width: {newRemarksWidth}");
                        }
                    }
                }
            }
            catch (Exception columnSaveException)
            {
                // Log error but don't crash application
                System.Diagnostics.Debug.WriteLine($"Error saving column widths: {columnSaveException.Message}");
            }
        }

        /// <summary>
        /// Saves application settings to settings.json file with proper formatting
        /// Creates human-readable JSON with indentation for easy manual editing
        /// </summary>
        public void SaveApplicationSettingsToFile()
        {
            try
            {
                // Serialize settings to formatted JSON for human readability
                string formattedSettingsJson = JsonConvert.SerializeObject(
                    currentApplicationSettings,
                    Newtonsoft.Json.Formatting.Indented,  // Pretty-print with indentation
                    new JsonSerializerSettings
                    {
                        // Add enum string conversion for readability
                        Converters = { new Newtonsoft.Json.Converters.StringEnumConverter() }
                    });

                // Write JSON to settings file atomically (write to temp file first, then rename)
                string tempSettingsFilePath = settingsFilePath + ".tmp";
                File.WriteAllText(tempSettingsFilePath, formattedSettingsJson);

                // Replace original file with temp file (atomic operation)
                if (File.Exists(settingsFilePath))
                    File.Delete(settingsFilePath);
                File.Move(tempSettingsFilePath, settingsFilePath);
                // Update Windows startup registry when settings are saved
                UpdateWindowsStartupRegistry();
            }
            catch (Exception settingsSaveException)
            {
                // Log error but don't crash application
                System.Diagnostics.Debug.WriteLine($"Error saving settings to file: {settingsSaveException.Message}");
            }
        }

        #endregion

        #region Device Management

        /// <summary>
        /// Checks if a device should be hidden from the list
        /// </summary>
        private bool IsDeviceHidden(string deviceId)
        {
            return currentApplicationSettings?.DeviceSettings?.ContainsKey(deviceId) == true &&
                   currentApplicationSettings.DeviceSettings[deviceId].IsHidden;
        }

        /// <summary>
        /// Gets or creates device settings for a specific device
        /// </summary>
        private DeviceSettings GetOrCreateDeviceSettings(string deviceId, string deviceName)
        {
            if (currentApplicationSettings?.DeviceSettings == null)
            {
                currentApplicationSettings.DeviceSettings = new Dictionary<string, DeviceSettings>();
            }

            if (!currentApplicationSettings.DeviceSettings.ContainsKey(deviceId))
            {
                currentApplicationSettings.DeviceSettings[deviceId] = new DeviceSettings
                {
                    DeviceId = deviceId,
                    DeviceName = deviceName
                };
            }

            return currentApplicationSettings.DeviceSettings[deviceId];
        }

        /// <summary>
        /// Hides a device from the list and saves settings
        /// </summary>
        private void HideDevice(string deviceId, string deviceName)
        {
            var deviceSettings = GetOrCreateDeviceSettings(deviceId, deviceName);
            deviceSettings.IsHidden = true;

            // Remove from UI immediately
            var deviceToRemove = AudioDeviceList.FirstOrDefault(d => d.AudioDevice?.Id.ToString() == deviceId);
            if (deviceToRemove != null)
            {
                AudioDeviceList.Remove(deviceToRemove);
            }

            // Update hidden count and save settings
            _ = RefreshAudioDeviceListFromSystem();
            SaveApplicationSettingsToFile();

            CurrentStatusMessage = $"Device '{deviceName}' has been hidden from the list";
        }

        /// <summary>
        /// Updates the count of hidden devices
        /// </summary>
        private void UpdateHiddenDeviceCount(List<CoreAudioDevice> allDevices)
        {
            hiddenDeviceCount = 0;

            if (currentApplicationSettings?.DeviceSettings != null)
            {
                foreach (var device in allDevices)
                {
                    if (IsDeviceHidden(device.Id.ToString()))
                    {
                        hiddenDeviceCount++;
                    }
                }
            }
        }

        /// <summary>
        /// Updates the hidden devices display message
        /// </summary>
        public void UpdateHiddenDevicesDisplay()
        {
            if (currentApplicationSettings?.Display?.ShowHiddenDeviceCount == true)
            {
                HiddenDevicesMessage = hiddenDeviceCount > 0
                    ? $"{hiddenDeviceCount} device(s) hidden from list"
                    : "";
                HiddenDevicesMessageVisibility = hiddenDeviceCount > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                HiddenDevicesMessageVisibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Saves device information to settings for all devices (visible and hidden)
        /// This ensures we can store remarks and hotkeys for any device
        /// </summary>
        private void SaveDeviceInfoToSettings(CoreAudioDevice device)
        {
            var deviceId = device.Id.ToString();
            var deviceSettings = GetOrCreateDeviceSettings(deviceId, device.FullName);

            // Update device name in case it changed
            deviceSettings.DeviceName = device.FullName;
            // Assign simplified ID if not already assigned
            if (string.IsNullOrEmpty(deviceSettings.SimplifiedId))
            {
                AssignSimplifiedIds();
            }
        }

        /// <summary>
        /// Updates the Windows Registry with current default device name
        /// Allows other programs to read the current audio device
        /// </summary>
        private void UpdateCurrentDeviceInRegistry(List<CoreAudioDevice> devices)
        {
            try
            {
                var defaultDevice = devices.FirstOrDefault(d => d.IsDefaultDevice);
                string currentDeviceName = defaultDevice?.FullName ?? "None";

                // Create or open registry key
                using (var key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY_PATH))
                {
                    if (key != null)
                    {
                        key.SetValue(REGISTRY_VALUE_NAME, currentDeviceName, RegistryValueKind.String);

                        // Also update current device simplified ID
                        var currentDeviceViewModel = AudioDeviceList.FirstOrDefault(d => d.IsCurrentlyActive);
                        string currentDeviceId = currentDeviceViewModel?.SimplifiedId ?? "None";
                        key.SetValue(REGISTRY_CURRENT_ID_VALUE_NAME, currentDeviceId, RegistryValueKind.String);

                        // Initialize switch command if not exists
                        if (key.GetValue(REGISTRY_SWITCH_VALUE_NAME) == null)
                        {
                            key.SetValue(REGISTRY_SWITCH_VALUE_NAME, "XX", RegistryValueKind.String);
                        }

                        System.Diagnostics.Debug.WriteLine($"Updated registry - Device: {currentDeviceName}, ID: {currentDeviceId}");
                    }
                }



            }
            catch (Exception registryException)
            {
                // Log error but don't crash application
                System.Diagnostics.Debug.WriteLine($"Error updating registry: {registryException.Message}");
            }
        }


        /// <summary>
        /// Monitors registry for external device switch commands
        /// </summary>
        private void OnRegistrySwitchMonitor_Tick(object sender, EventArgs e)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH))
                {
                    if (key != null)
                    {
                        string switchCommand = key.GetValue(REGISTRY_SWITCH_VALUE_NAME) as string;

                        if (!string.IsNullOrEmpty(switchCommand) && switchCommand != "XX")
                        {
                            // Valid switch command found - process it
                            ProcessRegistrySwitchCommand(switchCommand);

                            // Reset the command to neutral value
                            using (var writeKey = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY_PATH))
                            {
                                writeKey?.SetValue(REGISTRY_SWITCH_VALUE_NAME, "XX");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error monitoring registry switch: {ex.Message}");
            }
        }
        /// <summary>
        /// Processes a registry switch command by simplified ID
        /// </summary>
        private async void ProcessRegistrySwitchCommand(string simplifiedId)
        {
            try
            {
                // Find device with matching simplified ID
                var targetDevice = AudioDeviceList.FirstOrDefault(d => d.SimplifiedId == simplifiedId);

                if (targetDevice?.AudioDevice != null)
                {
                    CurrentStatusMessage = $"Switching to '{targetDevice.DeviceName}' via registry command...";

                    // Switch to the target device
                    await Task.Run(async () =>
                    {
                        try
                        {
                            await targetDevice.AudioDevice.SetAsDefaultAsync();
                        }
                        catch (MissingMethodException)
                        {
                            targetDevice.AudioDevice.SetAsDefault();
                        }
                    });

                    CurrentStatusMessage = $"Switched to '{targetDevice.DeviceName}' via registry command";
                    await RefreshAudioDeviceListFromSystem();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Registry switch command: Device with ID '{simplifiedId}' not found");
                }
            }
            catch (Exception ex)
            {
                CurrentStatusMessage = $"Registry switch failed: {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"Error processing registry switch command: {ex.Message}");
            }
        }

        /// <summary>
        /// Assigns simplified IDs to devices that don't have them
        /// </summary>
        private void AssignSimplifiedIds()
        {
            try
            {
                if (currentApplicationSettings?.DeviceSettings == null) return;

                // Get all existing simplified IDs to avoid duplicates
                var existingIds = currentApplicationSettings.DeviceSettings.Values
                    .Where(d => !string.IsNullOrEmpty(d.SimplifiedId))
                    .Select(d => d.SimplifiedId)
                    .ToHashSet();

                // Find next available ID number
                int nextIdNumber = 1;
                while (existingIds.Contains($"D{nextIdNumber}"))
                {
                    nextIdNumber++;
                }

                // Assign IDs to devices that don't have them
                foreach (var deviceSetting in currentApplicationSettings.DeviceSettings.Values)
                {
                    if (string.IsNullOrEmpty(deviceSetting.SimplifiedId))
                    {
                        deviceSetting.SimplifiedId = $"D{nextIdNumber}";
                        nextIdNumber++;

                        // Find next available number for next device
                        while (existingIds.Contains($"D{nextIdNumber}"))
                        {
                            nextIdNumber++;
                        }
                    }
                }

                // Save settings after assigning new IDs
                SaveApplicationSettingsToFile();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error assigning simplified IDs: {ex.Message}");
            }
        }

        #endregion

        #region Context Menu

        /// <summary>
        /// Creates and shows context menu for device operations
        /// </summary>
        private void ShowDeviceContextMenu(DeviceViewModel deviceViewModel, Point position)
        {
            var contextMenu = new ContextMenu();

            // Set context menu properties to ensure proper icon display
            contextMenu.HasDropShadow = true;
            contextMenu.Padding = new Thickness(4);

            // Hide from list option
            var hideMenuItem = new MenuItem();

            // Create a StackPanel to hold icon and text properly
            var hidePanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var hideIcon = new TextBlock
            {
                Text = "👁",
                FontSize = 16,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 24
            };

            var hideText = new TextBlock
            {
                Text = "Hide from list",
                VerticalAlignment = VerticalAlignment.Center
            };

            hidePanel.Children.Add(hideIcon);
            hidePanel.Children.Add(hideText);
            hideMenuItem.Header = hidePanel;
            hideMenuItem.Click += (s, e) => HideDevice(deviceViewModel.AudioDevice.Id.ToString(), deviceViewModel.DeviceName);
            contextMenu.Items.Add(hideMenuItem);

            // Set hotkey option
            var hotkeyMenuItem = new MenuItem();

            var hotkeyPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var hotkeyIcon = new TextBlock
            {
                Text = "⌨️",
                FontSize = 16,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 24
            };

            var hotkeyText = new TextBlock
            {
                Text = "Set Hot Key...",
                VerticalAlignment = VerticalAlignment.Center
            };

            hotkeyPanel.Children.Add(hotkeyIcon);
            hotkeyPanel.Children.Add(hotkeyText);
            hotkeyMenuItem.Header = hotkeyPanel;
            //hotkeyMenuItem.Click += (s, e) => ShowHotkeySetupWindow(deviceViewModel);
            //contextMenu.Items.Add(hotkeyMenuItem);

            //// Add separator
            //contextMenu.Items.Add(new Separator());


            hotkeyMenuItem.Click += (s, e) => ShowHotkeySetupWindow(deviceViewModel);
            contextMenu.Items.Add(hotkeyMenuItem);

            // Custom icon option
            var iconMenuItem = new MenuItem();

            var iconPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var iconIcon = new TextBlock
            {
                Text = "🎨",
                FontSize = 16,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 24
            };

            var iconText = new TextBlock
            {
                Text = "Custom Icon...",
                VerticalAlignment = VerticalAlignment.Center
            };

            iconPanel.Children.Add(iconIcon);
            iconPanel.Children.Add(iconText);
            iconMenuItem.Header = iconPanel;
            iconMenuItem.Click += (s, e) => ShowIconSelectionWindow(deviceViewModel);
            contextMenu.Items.Add(iconMenuItem);

            // Add separator
            contextMenu.Items.Add(new Separator());


            // Edit remarks option
            var remarksMenuItem = new MenuItem();

            var remarksPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                VerticalAlignment = VerticalAlignment.Center
            };

            var remarksIcon = new TextBlock
            {
                Text = "📝",
                FontSize = 16,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 24
            };

            var remarksText = new TextBlock
            {
                Text = "Edit Remarks...",
                VerticalAlignment = VerticalAlignment.Center
            };

            remarksPanel.Children.Add(remarksIcon);
            remarksPanel.Children.Add(remarksText);
            remarksMenuItem.Header = remarksPanel;
            remarksMenuItem.Click += (s, e) => ShowEditRemarksDialog(deviceViewModel);
            contextMenu.Items.Add(remarksMenuItem);

            // Show the context menu
            contextMenu.IsOpen = true;
        }

        /// <summary>
        /// Shows dialog to edit device remarks
        /// </summary>
        private void ShowEditRemarksDialog(DeviceViewModel deviceViewModel)
        {
            var deviceSettings = GetOrCreateDeviceSettings(
                deviceViewModel.AudioDevice.Id.ToString(),
                deviceViewModel.DeviceName);

            // Create simple input dialog
            var inputDialog = new Window
            {
                Title = "Edit Device Remarks",
                Width = 400,
                Height = 200,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this,
                ResizeMode = ResizeMode.NoResize
            };

            // Set icon using proper resource URI
            try
            {
                var iconUri = new Uri("pack://application:,,,/Resources/Icon.ico");
                inputDialog.Icon = new System.Windows.Media.Imaging.BitmapImage(iconUri);
            }
            catch (Exception)
            {
                // If icon fails to load, continue without it
                System.Diagnostics.Debug.WriteLine("Could not load icon for remarks dialog");
            }

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Label
            var label = new TextBlock
            {
                Text = $"Enter remarks for '{deviceViewModel.DeviceName}':",
                Margin = new Thickness(10, 10, 10, 5),
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(label, 0);
            grid.Children.Add(label);

            // TextBox
            var textBox = new TextBox
            {
                Text = deviceSettings.UserRemark ?? "",
                Margin = new Thickness(10, 5, 10, 10),
                AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto
            };
            Grid.SetRow(textBox, 1);
            grid.Children.Add(textBox);

            // Buttons panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(10)
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 75,
                Height = 25,
                Margin = new Thickness(0, 0, 10, 0),
                IsDefault = true
            };
            okButton.Click += (s, e) => { inputDialog.DialogResult = true; inputDialog.Close(); };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 75,
                Height = 25,
                IsCancel = true
            };
            cancelButton.Click += (s, e) => { inputDialog.DialogResult = false; inputDialog.Close(); };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            grid.Children.Add(buttonPanel);

            inputDialog.Content = grid;

            // Show dialog and process result
            if (inputDialog.ShowDialog() == true)
            {
                string newRemark = textBox.Text;
                deviceSettings.UserRemark = newRemark;

                // Update the view model to refresh display
                deviceViewModel.LoadDeviceSettings();

                // Save settings
                SaveApplicationSettingsToFile();

                CurrentStatusMessage = string.IsNullOrEmpty(newRemark)
                    ? $"Remarks cleared for '{deviceViewModel.DeviceName}'"
                    : $"Remarks updated for '{deviceViewModel.DeviceName}'";
            }
        }

        #endregion

        #region Global Hotkeys

        /// <summary>
        /// Handles global hotkey messages from Windows
        /// </summary>
        private IntPtr HandleGlobalHotkeyMessages(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_HOTKEY)
            {
                int hotkeyId = wParam.ToInt32();
                if (registeredHotkeys.ContainsKey(hotkeyId))
                {
                    string deviceId = registeredHotkeys[hotkeyId];
                    SwitchToDeviceByHotkey(deviceId);
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Switches to a device when its hotkey is pressed
        /// </summary>
        private async void SwitchToDeviceByHotkey(string deviceId)
        {
            try
            {
                var device = audioSystemController.GetPlaybackDevices(DeviceState.Active)
                    .FirstOrDefault(d => d.Id.ToString() == deviceId);

                if (device != null)
                {
                    await device.SetAsDefaultAsync();
                    CurrentStatusMessage = $"Switched to '{device.FullName}' via hotkey";
                    await RefreshAudioDeviceListFromSystem();
                }
            }
            catch (Exception ex)
            {
                CurrentStatusMessage = $"Hotkey switch failed: {ex.Message}";
            }
        }

        /// <summary>
        /// Registers a global hotkey for a device
        /// </summary>
        private bool RegisterDeviceHotkey(string deviceId, HotkeySettings hotkeySettings)
        {
            if (!hotkeySettings.IsEnabled || hotkeySettings.VirtualKeyCode == 0)
                return false;

            var hwnd = new WindowInteropHelper(this).Handle;
            int hotkeyId = nextHotkeyId++;

            if (RegisterHotKey(hwnd, hotkeyId, hotkeySettings.ModifierFlags, hotkeySettings.VirtualKeyCode))
            {
                registeredHotkeys[hotkeyId] = deviceId;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Registers all saved hotkeys on application startup
        /// </summary>
        private void RegisterAllHotkeys()
        {
            if (currentApplicationSettings?.DeviceSettings != null)
            {
                foreach (var kvp in currentApplicationSettings.DeviceSettings)
                {
                    if (kvp.Value.Hotkey.IsEnabled)
                    {
                        RegisterDeviceHotkey(kvp.Key, kvp.Value.Hotkey);
                    }
                }
            }
        }

        /// <summary>
        /// Unregisters all global hotkeys
        /// </summary>
        private void UnregisterAllHotkeys()
        {
            var hwnd = new WindowInteropHelper(this).Handle;

            foreach (var hotkeyId in registeredHotkeys.Keys.ToList())
            {
                UnregisterHotKey(hwnd, hotkeyId);
            }

            registeredHotkeys.Clear();
        }

        /// <summary>
        /// Shows the hotkey setup window for a device
        /// </summary>
        private void ShowHotkeySetupWindow(DeviceViewModel deviceViewModel)
        {
            var deviceSettings = GetOrCreateDeviceSettings(
                deviceViewModel.AudioDevice.Id.ToString(),
                deviceViewModel.DeviceName);

            var hotkeyWindow = new HotkeySetupWindow(deviceSettings.Hotkey, deviceViewModel.DeviceName);
            hotkeyWindow.Owner = this;

            if (hotkeyWindow.ShowDialog() == true)
            {
                // Unregister old hotkey if it exists
                UnregisterDeviceHotkey(deviceViewModel.AudioDevice.Id.ToString());

                // Update device settings with new hotkey
                deviceSettings.Hotkey = hotkeyWindow.HotkeySettings;

                // Register new hotkey if enabled
                if (deviceSettings.Hotkey.IsEnabled)
                {
                    RegisterDeviceHotkey(deviceViewModel.AudioDevice.Id.ToString(), deviceSettings.Hotkey);
                }

                // Update the view model to refresh hotkey display
                deviceViewModel.LoadDeviceSettings();

                // Save settings
                SaveApplicationSettingsToFile();

                CurrentStatusMessage = deviceSettings.Hotkey.IsEnabled
                    ? $"Hotkey set for '{deviceViewModel.DeviceName}'"
                    : $"Hotkey removed for '{deviceViewModel.DeviceName}'";
            }
        }


        /// <summary>
        /// Shows the icon selection window for a device
        /// </summary>
        private void ShowIconSelectionWindow(DeviceViewModel deviceViewModel)
        {
            var deviceSettings = GetOrCreateDeviceSettings(
                deviceViewModel.AudioDevice.Id.ToString(),
                deviceViewModel.DeviceName);

            var iconWindow = new IconSelectionWindow(deviceViewModel.DeviceName, deviceSettings.CustomIcon);
            iconWindow.Owner = this;

            if (iconWindow.ShowDialog() == true)
            {
                if (iconWindow.ResetToAutoDetect)
                {
                    // Clear custom icon to use auto-detection
                    deviceSettings.CustomIcon = "";
                    CurrentStatusMessage = $"Reset '{deviceViewModel.DeviceName}' to auto-detect icon";
                }
                else if (!string.IsNullOrEmpty(iconWindow.SelectedIcon))
                {
                    // Set custom icon
                    deviceSettings.CustomIcon = iconWindow.SelectedIcon;
                    CurrentStatusMessage = $"Custom icon set for '{deviceViewModel.DeviceName}'";
                }

                // Update the view model to refresh icon display
                deviceViewModel.UpdateFromAudioDevice(deviceViewModel.AudioDevice);

                // Save settings
                SaveApplicationSettingsToFile();
            }
        }

        /// <summary>
        /// Unregisters a hotkey for a specific device
        /// </summary>
        private void UnregisterDeviceHotkey(string deviceId)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            var hotkeyToRemove = registeredHotkeys.FirstOrDefault(kvp => kvp.Value == deviceId);

            if (hotkeyToRemove.Key != 0)
            {
                UnregisterHotKey(hwnd, hotkeyToRemove.Key);
                registeredHotkeys.Remove(hotkeyToRemove.Key);
            }
        }

        #endregion

        #region System Tray

        /// <summary>
        /// Initializes the system tray icon and context menu
        /// </summary>
        private void InitializeSystemTrayIcon()
        {
            try
            {
                // Create the tray icon
                trayIcon = new System.Windows.Forms.NotifyIcon();

                // Set the icon (using the same icon as the window)
                trayIcon.Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/Resources/Icon.ico")).Stream);

                // Set tooltip text
                trayIcon.Text = "Audio Device Switcher";

                // Handle double-click event to toggle window visibility
                trayIcon.MouseDoubleClick += OnTrayIcon_MouseDoubleClick;

                // Create and setup context menu
                CreateTrayContextMenu();

                // Show the tray icon
                trayIcon.Visible = true;
            }
            catch (Exception ex)
            {
                 System.Diagnostics.Debug.WriteLine($"Error initializing tray icon: {ex.Message}");

                      }
        }

        /// <summary>
        /// Creates the tray icon context menu
        /// </summary>
        private void CreateTrayContextMenu()
        {
            trayContextMenu = new System.Windows.Forms.ContextMenuStrip();

            // Show menu item
            var showMenuItem = new System.Windows.Forms.ToolStripMenuItem("Show...");
            showMenuItem.Click += OnTrayShow_Click;
            trayContextMenu.Items.Add(showMenuItem);

            // About menu item
            var aboutMenuItem = new System.Windows.Forms.ToolStripMenuItem("About...");
            aboutMenuItem.Click += OnTrayAbout_Click;
            trayContextMenu.Items.Add(aboutMenuItem);

            // Separator
            trayContextMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            // Exit menu item
            var exitMenuItem = new System.Windows.Forms.ToolStripMenuItem("Exit");
            exitMenuItem.Click += OnTrayExit_Click;
            trayContextMenu.Items.Add(exitMenuItem);

            // Assign context menu to tray icon
            trayIcon.ContextMenuStrip = trayContextMenu;


        }


        /// <summary>
        /// Handles tray icon double-click - toggles window visibility
        /// </summary>
        private void OnTrayIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            ToggleWindowVisibility();
        }

        /// <summary>
        /// Handles Show menu item click
        /// </summary>
        private void OnTrayShow_Click(object sender, EventArgs e)
        {
            ShowMainWindow();
        }

        /// <summary>
        /// Handles About menu item click
        /// </summary>
        private void OnTrayAbout_Click(object sender, EventArgs e)
        {
            ShowMainWindow(); // Ensure main window is visible first
            OnAboutClick(sender, new RoutedEventArgs());
        }

        /// <summary>
        /// Handles Exit menu item click
        /// </summary>
        private void OnTrayExit_Click(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Handles PayPal click from main window status bar
        /// </summary>
        private void OnMainPayPalClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.paypal.com/donate/?hosted_button_id=658JPTR7W5LNL",
                    UseShellExecute = true
                });

                CurrentStatusMessage = "Thank you for considering a donation!";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open PayPal: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        /// <summary>
        /// Initializes the donation reminder system
        /// </summary>
        private void InitializeDonationSystem()
        {
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY_PATH))
                {
                    if (key != null)
                    {
                        //// Check if user has already acknowledged donation request
                        //string donationStatus = key.GetValue(DONATION_REGISTRY_KEY) as string;
                        //if (donationStatus == DONATION_ACKNOWLEDGED_VALUE)
                        //{
                        //    // User has already acknowledged, don't show reminders
                        //    return;
                        //}

                        // Check if user has valid donor keys
                        // TODO: Implement your own validation logic here
                        if (false) // ValidateDonorKeys() - disabled for public release
                        {
                            System.Diagnostics.Debug.WriteLine("Valid donor keys found - donation reminder disabled");
                            return;
                        }


                        // Get or set first run date
                        DateTime firstRunDate;
                        string firstRunString = key.GetValue(FIRST_RUN_DATE_KEY) as string;

                        if (string.IsNullOrEmpty(firstRunString))
                        {
                            // First time running, record today's date
                            firstRunDate = DateTime.Now;
                            key.SetValue(FIRST_RUN_DATE_KEY, firstRunDate.ToBinary().ToString());
                        }
                        else
                        {
                            // Parse existing first run date
                            if (long.TryParse(firstRunString, out long binaryDate))
                            {
                                firstRunDate = DateTime.FromBinary(binaryDate);
                            }
                            else
                            {
                                // Corrupted date, reset to today
                                firstRunDate = DateTime.Now;
                                key.SetValue(FIRST_RUN_DATE_KEY, firstRunDate.ToBinary().ToString());
                            }
                        }

                        // Check if 30 days have passed
                        var daysSinceFirstRun = (DateTime.Now - firstRunDate).Days;
                        if (daysSinceFirstRun >= 30)
                        {
                            // Schedule donation reminder popup (random delay 10-60 seconds)
                            var random = new Random();
                            var delaySeconds = random.Next(10, 61); // 10 to 60 seconds

                            donationReminderTimer.Interval = TimeSpan.FromSeconds(delaySeconds);
                            donationReminderTimer.Start();
                          //  MessageBox.Show($"Timer started: {donationReminderTimer.IsEnabled}", "Timer Debug");

                            System.Diagnostics.Debug.WriteLine($"Donation reminder scheduled for {delaySeconds} seconds (user active for {daysSinceFirstRun} days)");
                          //  MessageBox.Show($"First Run: {firstRunDate:yyyy-MM-dd}\nDays: {daysSinceFirstRun}\nDelay: {delaySeconds}s", "Debug");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"User active for {daysSinceFirstRun} days, no donation reminder needed yet");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing donation system: {ex.Message}");
            }
        }


        /// <summary>
        /// Generates a valid donor key for given email (for validation purposes)
        /// </summary>
        private string GenerateDonorKey(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                    return null;

                string cleanEmail = email.Trim().ToLower();
                string timestamp = DateTime.Now.ToString("yyyyMM");
                string keySource = $"{cleanEmail}{SECRET_PHRASE_1}{timestamp}";

                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(keySource));
                    var keyHash = Convert.ToBase64String(hashBytes).Substring(0, 16).Replace("+", "A").Replace("/", "B");
                    return $"DONOR-EMAIL-{keyHash}";
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Generates donor key with specific timestamp (for checking previous month)
        /// </summary>
        private string GenerateDonorKeyWithTimestamp(string email, string timestamp)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                    return null;

                string cleanEmail = email.Trim().ToLower();
                string keySource = $"{cleanEmail}{SECRET_PHRASE_1}{timestamp}";

                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(keySource));
                    var keyHash = Convert.ToBase64String(hashBytes).Substring(0, 16).Replace("+", "A").Replace("/", "B");
                    return $"DONOR-EMAIL-{keyHash}";
                }
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Validates if stored donor keys are authentic
        /// </summary>
        /// <summary>
        /// Validates if stored email and donor keys are authentic
        /// </summary>
        private bool ValidateDonorKeys()
        {
            try
            {
                string primaryEmail = null;
                string primaryKey = null;
                string secondaryEmail = null;
                string secondaryKey = null;

                // Read from primary location
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH))
                {
                    primaryEmail = key?.GetValue(DONOR_EMAIL_PRIMARY) as string;
                    primaryKey = key?.GetValue(DONOR_KEY_PRIMARY) as string;
                }

                // Read from secondary location  
                using (var key = Registry.CurrentUser.OpenSubKey(DONOR_KEY_ALT_PATH))
                {
                    secondaryEmail = key?.GetValue(DONOR_EMAIL_SECONDARY) as string;
                    secondaryKey = key?.GetValue(DONOR_KEY_SECONDARY) as string;
                }

                // Both locations must have email and key, and they must match
                if (string.IsNullOrEmpty(primaryEmail) || string.IsNullOrEmpty(primaryKey) ||
                    string.IsNullOrEmpty(secondaryEmail) || string.IsNullOrEmpty(secondaryKey))
                    return false;

                if (primaryEmail != secondaryEmail || primaryKey != secondaryKey)
                    return false;

                // Validate email and key combination
                return ValidateEmailAndKey(primaryEmail, primaryKey);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validates email and key combination
        /// </summary>
        private bool ValidateEmailAndKey(string email, string key)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(key))
                    return false;

                if (!key.StartsWith("DONOR-EMAIL-"))
                    return false;

                // Check current month key
                var currentKey = GenerateDonorKey(email);
                if (key == currentKey)
                    return true;

                // Check previous month key (in case month changed)
                var prevMonth = DateTime.Now.AddMonths(-1);
                var prevTimestamp = prevMonth.ToString("yyyyMM");
                var prevKey = GenerateDonorKeyWithTimestamp(email, prevTimestamp);

                return key == prevKey;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Removes donor keys from both registry locations (for testing/unregistration)
        /// </summary>
        private void ClearDonorKeys()
        {
            try
            {
                // Clear from primary location
                using (var regKey = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY_PATH, true))
                {
                    regKey?.DeleteValue(DONOR_EMAIL_PRIMARY, false);
                    regKey?.DeleteValue(DONOR_KEY_PRIMARY, false);
                }

                // Clear from secondary location
                using (var regKey = Registry.CurrentUser.OpenSubKey(DONOR_KEY_ALT_PATH, true))
                {
                    regKey?.DeleteValue(DONOR_EMAIL_SECONDARY, false);
                    regKey?.DeleteValue(DONOR_KEY_SECONDARY, false);
                }

                System.Diagnostics.Debug.WriteLine("Donor keys cleared from registry");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing donor keys: {ex.Message}");
            }
        }

        /// <summary>
        /// Public method for UI to validate and store donor registration
        /// Returns: true if valid and stored, false if invalid
        /// </summary>
        public bool RegisterDonorKey(string email, string key)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(key))
                    return false;

                // Clean email format
                string cleanEmail = email.Trim().ToLower();

                // Validate email format (basic check)
                if (!cleanEmail.Contains("@") || !cleanEmail.Contains("."))
                    return false;

                // Validate key format and authenticity
                if (!ValidateEmailAndKey(cleanEmail, key.Trim()))
                    return false;

                // Store valid keys
                if (StoreDonorKeys(cleanEmail, key.Trim()))
                {
                    CurrentStatusMessage = "Donor registration successful! Thank you for your support.";
                    System.Diagnostics.Debug.WriteLine($"Donor registered: {cleanEmail}");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registering donor key: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if current user is a registered donor (for UI status)
        /// </summary>
        public bool IsRegisteredDonor()
        {
            return ValidateDonorKeys();
        }

        /// <summary>
        /// Stores valid email and key in dual registry locations
        /// </summary>
        private bool StoreDonorKeys(string email, string key)
        {
            try
            {
                if (!ValidateEmailAndKey(email, key))
                    return false;

                // Store in primary location
                using (var regKey = Registry.CurrentUser.CreateSubKey(REGISTRY_KEY_PATH))
                {
                    regKey?.SetValue(DONOR_EMAIL_PRIMARY, email);
                    regKey?.SetValue(DONOR_KEY_PRIMARY, key);
                }

                // Store in secondary location
                using (var regKey = Registry.CurrentUser.CreateSubKey(DONOR_KEY_ALT_PATH))
                {
                    regKey?.SetValue(DONOR_EMAIL_SECONDARY, email);
                    regKey?.SetValue(DONOR_KEY_SECONDARY, key);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }


        /// <summary>
        /// Handles donation reminder timer tick - shows donation popup
        /// </summary>
        private void OnDonationReminder_Tick(object sender, EventArgs e)
        {
         //   MessageBox.Show("Timer tick fired!", "Timer Tick");
            try
            {
                // Stop the timer (one-time popup per session)
                donationReminderTimer.Stop();

                // Show donation popup if not already shown
                if (!isDonationDialogShown)
                {
                    isDonationDialogShown = true;
                    ShowDonationPopup();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing donation reminder: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the donation reminder popup window
        /// </summary>
        private void ShowDonationPopup()
        {
            try
            {
              //  MessageBox.Show("Creating donation window...", "Debug");
                var donationWindow = new DonationReminderWindow();

                // Only set owner if main window is visible
                if (this.IsVisible && this.WindowState != WindowState.Minimized)
                {
                    donationWindow.Owner = this;
                }

            //    MessageBox.Show("Showing dialog...", "Debug");
                var result = donationWindow.ShowDialog();

                if (result == true)
                {
                    CurrentStatusMessage = "Thank you for considering a donation!";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Popup Error: {ex.Message}", "Error");
                System.Diagnostics.Debug.WriteLine($"Error showing donation popup: {ex.Message}");
            }
        }

        /// <summary>
        /// Handles Settings button click
        /// </summary>
        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWindow = new SettingsWindow(this);
                settingsWindow.Owner = this.IsVisible ? this : null;
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening settings: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Updates the tray icon tooltip with current device information
        /// </summary>
        private void UpdateTrayIconTooltip()
        {
            try
            {
                if (trayIcon != null)
                {
                    var currentDevice = AudioDeviceList.FirstOrDefault(d => d.IsCurrentlyActive);

                    if (currentDevice != null)
                    {
                        string tooltipText = $"Audio Device Switcher\n{currentDevice.DeviceName}\nID: {currentDevice.SimplifiedId}";
                        trayIcon.Text = tooltipText;
                    }
                    else
                    {
                        trayIcon.Text = "Audio Device Switcher\nNo active device";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating tray tooltip: {ex.Message}");
            }
        }

        /// <summary>
        /// Toggles main window visibility
        /// </summary>
        private void ToggleWindowVisibility()
        {
            if (this.IsVisible && this.WindowState != WindowState.Minimized)
            {
                HideMainWindow();
            }
            else
            {
                ShowMainWindow();
            }
        }

        /// <summary>
        /// Shows and activates the main window
        /// </summary>
        private void ShowMainWindow()
        {
            this.Show();
            this.WindowState = WindowState.Normal;
            this.Activate();
            this.Focus();
        }

        /// <summary>
        /// Hides the main window
        /// </summary>
        private void HideMainWindow()
        {
            this.Hide();
        }

        /// <summary>
        /// Handles window state changes - hide to tray when minimized
        /// </summary>
        protected override void OnStateChanged(EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.Hide();
            }

            base.OnStateChanged(e);
        }

        /// <summary>
        /// Determines if the application should start in tray mode
        /// </summary>
        public bool ShouldStartInTray()
        {
            return currentApplicationSettings?.System?.StartInTray ?? false;
        }

        /// <summary>
        /// Updates Windows startup registry based on StartWithWindows setting
        /// </summary>
        public void UpdateWindowsStartupRegistry()
        {
            try
            {
                bool shouldStartWithWindows = currentApplicationSettings?.System?.StartWithWindows ?? false;
                string appName = "AudioDeviceSwitcher";
                string executablePath = System.Reflection.Assembly.GetExecutingAssembly().Location;

                using (var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (shouldStartWithWindows)
                    {
                        // Add to startup
                        key?.SetValue(appName, $"\"{executablePath}\"");
                        System.Diagnostics.Debug.WriteLine("Added application to Windows startup");
                    }
                    else
                    {
                        // Remove from startup
                        if (key?.GetValue(appName) != null)
                        {
                            key.DeleteValue(appName);
                            System.Diagnostics.Debug.WriteLine("Removed application from Windows startup");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating Windows startup registry: {ex.Message}");
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        /// <summary>
        /// View model class that represents an audio device in the UI
        /// Implements INotifyPropertyChanged for automatic UI updates when properties change
        /// </summary>
        public class DeviceViewModel : INotifyPropertyChanged
        {
            // Private backing fields for properties
            private string deviceName;
            private string deviceStatus;
            private bool isCurrentlyActive;
            private string deviceTypeIcon;
            private string assignedHotkey;
            private string userRemarks;
            private string simplifiedId;
            /// <summary>
            /// Display name of the audio device shown to user
            /// </summary>
            public string DeviceName
            {
                get => deviceName;
                set { deviceName = value; NotifyPropertyChanged(); }
            }

            /// <summary>
            /// Current status of the device (Active/Available)
            /// </summary>
            public string DeviceStatus
            {
                get => deviceStatus;
                set { deviceStatus = value; NotifyPropertyChanged(); }
            }

            /// <summary>
            /// Whether this device is currently the system default (for UI highlighting)
            /// </summary>
            public bool IsCurrentlyActive
            {
                get => isCurrentlyActive;
                set { isCurrentlyActive = value; NotifyPropertyChanged(); }
            }

            /// <summary>
            /// Unicode icon character representing the device type (headphones, speakers, etc.)
            /// </summary>
            public string DeviceTypeIcon
            {
                get => deviceTypeIcon;
                set { deviceTypeIcon = value; NotifyPropertyChanged(); }
            }

            /// <summary>
            /// Assigned hotkey combination for this device (or "None")
            /// </summary>
            public string AssignedHotkey
            {
                get => assignedHotkey;
                set { assignedHotkey = value; NotifyPropertyChanged(); }
            }

            /// <summary>
            /// User-defined remarks/notes for this device
            /// </summary>
            public string UserRemarks
            {
                get => userRemarks;
                set { userRemarks = value; NotifyPropertyChanged(); }
            }

            /// <summary>
            /// Simplified unique identifier for this device (D1, D2, etc.)
            /// </summary>
            public string SimplifiedId
            {
                get => simplifiedId;
                set { simplifiedId = value; NotifyPropertyChanged(); }
            }

            /// <summary>
            /// Reference to the actual CoreAudioDevice object from AudioSwitcher API
            /// </summary>
            public CoreAudioDevice AudioDevice { get; set; }

            /// <summary>
            /// Reference to parent window for context menu operations
            /// </summary>
            public MainWindow ParentWindow { get; set; }

            /// <summary>
            /// Constructor - creates view model from CoreAudioDevice
            /// </summary>
            public DeviceViewModel(CoreAudioDevice coreAudioDevice, MainWindow parentWindow = null)
            {
                ParentWindow = parentWindow;
                UpdateFromAudioDevice(coreAudioDevice);
                LoadDeviceSettings();
            }

            /// <summary>
            /// Updates this view model with latest information from CoreAudioDevice
            /// Called when device properties change in the system
            /// </summary>
            public void UpdateFromAudioDevice(CoreAudioDevice coreAudioDevice)
            {
                AudioDevice = coreAudioDevice;
                DeviceName = coreAudioDevice.FullName;
                IsCurrentlyActive = coreAudioDevice.IsDefaultDevice;
                DeviceStatus = coreAudioDevice.IsDefaultDevice ? "Active" : "Available";

                // Determine and set appropriate icon based on device type
                DeviceTypeIcon = DetermineDeviceTypeIcon(coreAudioDevice, ParentWindow);


                // Load device-specific settings (hotkey and remarks)
                LoadDeviceSettings();
            }

            /// <summary>
            /// Loads device-specific settings like hotkey and remarks from parent window settings
            /// </summary>
            public void LoadDeviceSettings()
            {
                if (ParentWindow?.currentApplicationSettings?.DeviceSettings != null && AudioDevice != null)
                {
                    var deviceId = AudioDevice.Id.ToString();
                    if (ParentWindow.currentApplicationSettings.DeviceSettings.ContainsKey(deviceId))
                    {
                        var settings = ParentWindow.currentApplicationSettings.DeviceSettings[deviceId];

                        // Load hotkey display
                        if (settings.Hotkey.IsEnabled && !string.IsNullOrEmpty(settings.Hotkey.Key))
                        {
                            var modifiers = string.Join(" + ", settings.Hotkey.Modifiers);
                            AssignedHotkey = string.IsNullOrEmpty(modifiers)
                                ? settings.Hotkey.Key
                                : $"{modifiers} + {settings.Hotkey.Key}";
                        }
                        else
                        {
                            AssignedHotkey = "None";
                        }

                        // Load user remarks
                        UserRemarks = settings.UserRemark ?? "";
                        // Load simplified ID
                        SimplifiedId = settings.SimplifiedId ?? "";
                        // Refresh icon (will use custom icon if set)
                        DeviceTypeIcon = DetermineDeviceTypeIcon(AudioDevice, ParentWindow);

                    }
                    else
                    {
                        AssignedHotkey = "None";
                        UserRemarks = "";
                        // Update icon (will use auto-detection since no custom icon)
                        if (AudioDevice != null)
                            DeviceTypeIcon = DetermineDeviceTypeIcon(AudioDevice, ParentWindow); 
                    }
                }
                else
                {
                    AssignedHotkey = "None";
                    UserRemarks = "";
                    // Update icon (will use auto-detection since no custom icon)
                    if (AudioDevice != null)
                        DeviceTypeIcon = DetermineDeviceTypeIcon(AudioDevice, ParentWindow);
                }
            }

            
            /// <summary>
            /// Analyzes device name and properties to determine appropriate icon
            /// Returns Unicode character representing device type for display
            /// Checks for custom user icon first, then falls back to auto-detection
            /// </summary>
            public static string DetermineDeviceTypeIcon(CoreAudioDevice audioDevice, MainWindow parentWindow)
            {
                // Check for custom user-selected icon first
                if (parentWindow?.currentApplicationSettings?.DeviceSettings != null)
                {
                    var deviceId = audioDevice.Id.ToString();
                    if (parentWindow.currentApplicationSettings.DeviceSettings.ContainsKey(deviceId))
                    {
                        var customIcon = parentWindow.currentApplicationSettings.DeviceSettings[deviceId].CustomIcon;
                        if (!string.IsNullOrEmpty(customIcon))
                        {
                            return customIcon;
                        }
                    }
                }

                // Get device name in lowercase for easier pattern matching
                string deviceNameLower = audioDevice.FullName?.ToLowerInvariant() ?? "";

                // Check for headphones/headset indicators
                if (deviceNameLower.Contains("headphone") ||
                    deviceNameLower.Contains("headset") ||
                    deviceNameLower.Contains("earphone") ||
                    deviceNameLower.Contains("earbud") ||
                    deviceNameLower.Contains("airpods") ||
                    deviceNameLower.Contains("beats") ||
                    deviceNameLower.Contains("sony wh") ||
                    deviceNameLower.Contains("bose qc"))
                {
                    return "🎧"; // Headphones icon
                }

                // Check for monitor/display audio
                if (deviceNameLower.Contains("monitor") ||
                    deviceNameLower.Contains("display") ||
                    deviceNameLower.Contains("lg") ||
                    deviceNameLower.Contains("samsung") ||
                    deviceNameLower.Contains("dell") ||
                    deviceNameLower.Contains("asus") ||
                    deviceNameLower.Contains("acer") ||
                    deviceNameLower.Contains("hdmi") ||
                    deviceNameLower.Contains("displayport"))
                {
                    return "🖥️"; // Monitor icon
                }

                // Check for bluetooth devices
                if (deviceNameLower.Contains("bluetooth") ||
                    deviceNameLower.Contains("wireless") ||
                    deviceNameLower.Contains("bt"))
                {
                    return "📶"; // Wireless signal icon
                }

                // Check for USB devices
                if (deviceNameLower.Contains("usb") ||
                    deviceNameLower.Contains("gaming") ||
                    deviceNameLower.Contains("webcam") ||
                    deviceNameLower.Contains("microphone"))
                {
                    return "🔌"; // USB/Plug icon
                }

                // Check for built-in/realtek/internal audio
                if (deviceNameLower.Contains("realtek") ||
                    deviceNameLower.Contains("built-in") ||
                    deviceNameLower.Contains("internal") ||
                    deviceNameLower.Contains("onboard") ||
                    deviceNameLower.Contains("motherboard") ||
                    deviceNameLower.Contains("integrated"))
                {
                    return "💻"; // Computer icon for built-in audio
                }

                // Check for external speakers
                if (deviceNameLower.Contains("speaker") ||
                    deviceNameLower.Contains("logitech") ||
                    deviceNameLower.Contains("creative") ||
                    deviceNameLower.Contains("jbl") ||
                    deviceNameLower.Contains("harman"))
                {
                    return "🔊"; // Speaker icon
                }

                // Default fallback icon for unrecognized devices
                return "🎵"; // Musical note for generic audio device
            }

            // INotifyPropertyChanged implementation for automatic UI binding updates
            public event PropertyChangedEventHandler PropertyChanged;

            /// <summary>
            /// Notifies UI that a property has changed so binding can update display
            /// Uses CallerMemberName so we don't have to specify property name manually
            /// </summary>
            protected virtual void NotifyPropertyChanged([CallerMemberName] string changedPropertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(changedPropertyName));
            }
        }
    }
}