using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace AudioDeviceSwitcher
{
    public partial class HotkeySetupWindow : Window
    {
        /// <summary>
        /// The hotkey settings being configured
        /// </summary>
        public HotkeySettings HotkeySettings { get; private set; }

        /// <summary>
        /// Available keys for hotkey selection
        /// </summary>
        private readonly Dictionary<string, uint> availableKeys = new Dictionary<string, uint>
        {
            // Function keys
            { "F1", 0x70 }, { "F2", 0x71 }, { "F3", 0x72 }, { "F4", 0x73 },
            { "F5", 0x74 }, { "F6", 0x75 }, { "F7", 0x76 }, { "F8", 0x77 },
            { "F9", 0x78 }, { "F10", 0x79 }, { "F11", 0x7A }, { "F12", 0x7B },
            
            // Number keys
            { "1", 0x31 }, { "2", 0x32 }, { "3", 0x33 }, { "4", 0x34 },
            { "5", 0x35 }, { "6", 0x36 }, { "7", 0x37 }, { "8", 0x38 },
            { "9", 0x39 }, { "0", 0x30 },
            
            // Letter keys
            { "A", 0x41 }, { "B", 0x42 }, { "C", 0x43 }, { "D", 0x44 },
            { "E", 0x45 }, { "F", 0x46 }, { "G", 0x47 }, { "H", 0x48 },
            { "I", 0x49 }, { "J", 0x4A }, { "K", 0x4B }, { "L", 0x4C },
            { "M", 0x4D }, { "N", 0x4E }, { "O", 0x4F }, { "P", 0x50 },
            { "Q", 0x51 }, { "R", 0x52 }, { "S", 0x53 }, { "T", 0x54 },
            { "U", 0x55 }, { "V", 0x56 }, { "W", 0x57 }, { "X", 0x58 },
            { "Y", 0x59 }, { "Z", 0x5A },
            
            // Special keys
            { "Space", 0x20 }, { "Enter", 0x0D }, { "Tab", 0x09 },
            { "Esc", 0x1B }, { "Insert", 0x2D }, { "Delete", 0x2E },
            { "Home", 0x24 }, { "End", 0x23 }, { "Page Up", 0x21 }, { "Page Down", 0x22 },
            { "Left Arrow", 0x25 }, { "Up Arrow", 0x26 }, { "Right Arrow", 0x27 }, { "Down Arrow", 0x28 }
        };

        public HotkeySetupWindow(HotkeySettings currentHotkey, string deviceName)
        {
            InitializeComponent();

            // Initialize the hotkey settings (create copy to avoid modifying original)
            HotkeySettings = new HotkeySettings
            {
                IsEnabled = currentHotkey.IsEnabled,
                Modifiers = new List<string>(currentHotkey.Modifiers),
                Key = currentHotkey.Key,
                VirtualKeyCode = currentHotkey.VirtualKeyCode,
                ModifierFlags = currentHotkey.ModifierFlags
            };

            // Set device name
            DeviceNameText.Text = $"Device: {deviceName}";

            // Initialize UI
            InitializeControls();
            LoadCurrentSettings();
            UpdateHotkeyDisplay();
            UpdateControlStates();
        }

        /// <summary>
        /// Initializes the UI controls with available options
        /// </summary>
        private void InitializeControls()
        {
            // Populate key combobox with available keys
            KeyComboBox.ItemsSource = availableKeys.Keys.ToList();

            // Set up event handlers for real-time hotkey display updates
            CtrlCheckBox.Checked += OnHotkeyChanged;
            CtrlCheckBox.Unchecked += OnHotkeyChanged;
            AltCheckBox.Checked += OnHotkeyChanged;
            AltCheckBox.Unchecked += OnHotkeyChanged;
            ShiftCheckBox.Checked += OnHotkeyChanged;
            ShiftCheckBox.Unchecked += OnHotkeyChanged;
            KeyComboBox.SelectionChanged += OnHotkeyChanged;
        }

        /// <summary>
        /// Loads current hotkey settings into the UI controls
        /// </summary>
        private void LoadCurrentSettings()
        {
            EnableHotkeyCheckBox.IsChecked = HotkeySettings.IsEnabled;

            // Load modifiers
            CtrlCheckBox.IsChecked = HotkeySettings.Modifiers.Contains("Ctrl");
            AltCheckBox.IsChecked = HotkeySettings.Modifiers.Contains("Alt");
            ShiftCheckBox.IsChecked = HotkeySettings.Modifiers.Contains("Shift");

            // Load key
            if (!string.IsNullOrEmpty(HotkeySettings.Key))
            {
                KeyComboBox.SelectedItem = HotkeySettings.Key;
            }
        }

        /// <summary>
        /// Handles changes to the enable hotkey checkbox
        /// </summary>
        private void OnEnableHotkeyChanged(object sender, RoutedEventArgs e)
        {
            UpdateControlStates();
            UpdateHotkeyDisplay();
        }

        /// <summary>
        /// Handles changes to hotkey components
        /// </summary>
        private void OnHotkeyChanged(object sender, EventArgs e)
        {
            UpdateHotkeyDisplay();
        }

        /// <summary>
        /// Updates the enabled state of hotkey configuration controls
        /// </summary>
        private void UpdateControlStates()
        {
            bool isEnabled = EnableHotkeyCheckBox.IsChecked == true;
            HotkeyConfigPanel.IsEnabled = isEnabled;
        }

        /// <summary>
        /// Updates the current hotkey display text
        /// </summary>
        private void UpdateHotkeyDisplay()
        {
            if (EnableHotkeyCheckBox.IsChecked != true)
            {
                CurrentHotkeyText.Text = "None";
                return;
            }

            var parts = new List<string>();

            // Add modifiers
            if (CtrlCheckBox.IsChecked == true) parts.Add("Ctrl");
            if (AltCheckBox.IsChecked == true) parts.Add("Alt");
            if (ShiftCheckBox.IsChecked == true) parts.Add("Shift");

            // Add main key
            if (KeyComboBox.SelectedItem is string selectedKey)
            {
                parts.Add(selectedKey);
            }

            CurrentHotkeyText.Text = parts.Count > 0 ? string.Join(" + ", parts) : "None";
        }

        /// <summary>
        /// Handles OK button click - validates and saves hotkey settings
        /// </summary>
        private void OnOkClicked(object sender, RoutedEventArgs e)
        {
            if (ValidateHotkeySettings())
            {
                SaveHotkeySettings();
                DialogResult = true;
                Close();
            }
        }

        /// <summary>
        /// Handles Cancel button click
        /// </summary>
        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Validates the current hotkey configuration
        /// </summary>
        private bool ValidateHotkeySettings()
        {
            if (EnableHotkeyCheckBox.IsChecked != true)
            {
                return true; // Disabled hotkey is always valid
            }

            // Check if at least one modifier is selected
            bool hasModifier = CtrlCheckBox.IsChecked == true ||
                              AltCheckBox.IsChecked == true ||
                              ShiftCheckBox.IsChecked == true;

            if (!hasModifier)
            {
                MessageBox.Show("Please select at least one modifier key (Ctrl, Alt, or Shift).",
                    "Invalid Hotkey", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Check if a main key is selected
            if (KeyComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select a main key for the hotkey combination.",
                    "Invalid Hotkey", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Saves the configured hotkey settings
        /// </summary>
        private void SaveHotkeySettings()
        {
            HotkeySettings.IsEnabled = EnableHotkeyCheckBox.IsChecked == true;

            if (HotkeySettings.IsEnabled)
            {
                // Save modifiers
                HotkeySettings.Modifiers.Clear();
                if (CtrlCheckBox.IsChecked == true) HotkeySettings.Modifiers.Add("Ctrl");
                if (AltCheckBox.IsChecked == true) HotkeySettings.Modifiers.Add("Alt");
                if (ShiftCheckBox.IsChecked == true) HotkeySettings.Modifiers.Add("Shift");

                // Save main key
                HotkeySettings.Key = KeyComboBox.SelectedItem as string ?? "";

                // Calculate virtual key code and modifier flags for Windows API
                if (availableKeys.ContainsKey(HotkeySettings.Key))
                {
                    HotkeySettings.VirtualKeyCode = availableKeys[HotkeySettings.Key];
                }

                HotkeySettings.ModifierFlags = 0;
                if (CtrlCheckBox.IsChecked == true) HotkeySettings.ModifierFlags |= 0x0002; // MOD_CONTROL
                if (AltCheckBox.IsChecked == true) HotkeySettings.ModifierFlags |= 0x0001;  // MOD_ALT
                if (ShiftCheckBox.IsChecked == true) HotkeySettings.ModifierFlags |= 0x0004; // MOD_SHIFT
            }
            else
            {
                // Clear settings when disabled
                HotkeySettings.Modifiers.Clear();
                HotkeySettings.Key = "";
                HotkeySettings.VirtualKeyCode = 0;
                HotkeySettings.ModifierFlags = 0;
            }
        }
    }
}