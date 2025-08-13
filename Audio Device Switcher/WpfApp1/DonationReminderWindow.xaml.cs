using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace AudioDeviceSwitcher
{
    public partial class DonationReminderWindow : Window
    {
        /// <summary>
        /// Whether the user chose "Don't show again"
        /// </summary>
        public bool DontShowAgain { get; private set; } = false;

        public DonationReminderWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles PayPal section click
        /// </summary>
        private void OnPayPalClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.paypal.com/donate/?hosted_button_id=658JPTR7W5LNL",
                    UseShellExecute = true
                });

                // Close the dialog after opening PayPal
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open PayPal: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles "Maybe Later" button click
        /// </summary>
        private void OnLaterClicked(object sender, RoutedEventArgs e)
        {
          //  DontShowAgain = DontShowAgainCheckBox.IsChecked == true;
            DialogResult = true;
            Close();
        }

        /// <summary>
        /// Handles "Thank You!" button click - same as PayPal click
        /// </summary>
        private void OnThankYouClicked(object sender, RoutedEventArgs e)
        {
         //   DontShowAgain = DontShowAgainCheckBox.IsChecked == true;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://www.paypal.com/donate/?hosted_button_id=658JPTR7W5LNL",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open PayPal: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            DialogResult = true;
            Close();
        }
    }
}