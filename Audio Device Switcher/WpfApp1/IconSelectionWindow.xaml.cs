using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AudioDeviceSwitcher
{
    public partial class IconSelectionWindow : Window
    {
        /// <summary>
        /// Available device icons for selection
        /// </summary>
        private readonly Dictionary<string, string> availableIcons = new Dictionary<string, string>
        {
            { "🎧", "Headphones" },
            { "🔊", "Speakers" },
            { "🖥️", "Monitor" },
            { "📶", "Wireless" },
            { "🔌", "USB Device" },
            { "💻", "Built-in Audio" },
            { "🎵", "Generic Audio" },
            { "🎤", "Microphone" },
            { "📻", "Radio/Receiver" },
            { "📺", "TV/Display" },
            { "🎮", "Gaming Device" },
            { "📱", "Mobile Device" },
            { "🏠", "Home Audio" },
            { "🎼", "Music System" },
            { "🔈", "Volume Low" },
            { "🔉", "Volume Medium" },
            { "🎛️", "Audio Mixer" },
            { "🎚️", "Audio Control" }
        };

        /// <summary>
        /// The selected icon
        /// </summary>
        public string SelectedIcon { get; private set; } = "";

        /// <summary>
        /// Whether to reset to auto-detection
        /// </summary>
        public bool ResetToAutoDetect { get; private set; } = false;

        public IconSelectionWindow(string deviceName, string currentIcon)
        {
            InitializeComponent();

            DeviceNameText.Text = $"Device: {deviceName}";
            SelectedIcon = currentIcon;

            CreateIconButtons();
        }

        /// <summary>
        /// Creates clickable icon buttons in the selection panel
        /// </summary>
        private void CreateIconButtons()
        {
            foreach (var iconPair in availableIcons)
            {
                var button = new Button
                {
                    Content = iconPair.Key,
                    FontSize = 24,
                    Width = 50,
                    Height = 50,
                    Margin = new Thickness(5),
                    ToolTip = iconPair.Value,
                    Tag = iconPair.Key,
                    Background = Brushes.White,
                    BorderBrush = new SolidColorBrush(Color.FromRgb(209, 213, 219)),
                    BorderThickness = new Thickness(1),
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                // Handle click
                button.Click += OnIconButtonClick;

                // Highlight if this is the current icon
                if (iconPair.Key == SelectedIcon)
                {
                    button.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246)); // Blue
                    button.Foreground = Brushes.White;
                }

                IconSelectionPanel.Children.Add(button);
            }
        }

        /// <summary>
        /// Handles icon button click
        /// </summary>
        private void OnIconButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                // Reset all buttons to default style
                foreach (Button button in IconSelectionPanel.Children)
                {
                    button.Background = Brushes.White;
                    button.Foreground = Brushes.Black;
                }

                // Highlight selected button
                clickedButton.Background = new SolidColorBrush(Color.FromRgb(59, 130, 246));
                clickedButton.Foreground = Brushes.White;

                // Store selection
                SelectedIcon = clickedButton.Tag.ToString();
            }
        }

        /// <summary>
        /// Handles Reset button click
        /// </summary>
        private void OnResetClicked(object sender, RoutedEventArgs e)
        {
            ResetToAutoDetect = true;
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Handles OK button click
        /// </summary>
        private void OnOkClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Handles Cancel button click
        /// </summary>
        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}