using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Input;
using System.Windows.Navigation;

namespace AudioDeviceSwitcher
{
    /// <summary>
    /// Interaction logic for AboutWindow.xaml
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            LoadAssemblyInformation();
        }

        /// <summary>
        /// Loads application information from Assembly attributes
        /// </summary>
        private void LoadAssemblyInformation()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();

                // Get program title from AssemblyTitle attribute
                var titleAttribute = assembly.GetCustomAttribute<AssemblyTitleAttribute>();
                ProgramNameText.Text = titleAttribute?.Title ?? "Audio Device Switcher";

                // Get description from AssemblyDescription attribute
                var descriptionAttribute = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>();
                ProgramDescriptionText.Text = descriptionAttribute?.Description ?? "Quick and easy audio device switching with global hotkeys";

                // Get version from AssemblyVersion
                var version = assembly.GetName().Version;
                VersionText.Text = $"v {version?.ToString() ?? "1.0.0.0"}";

                // Update window title with version
                this.Title = $"About {ProgramNameText.Text} {VersionText.Text}";
            }
            catch (Exception ex)
            {
                // Fallback to default values if assembly reading fails
                System.Diagnostics.Debug.WriteLine($"Error loading assembly info: {ex.Message}");
                ProgramNameText.Text = "Audio Device Switcher";
                ProgramDescriptionText.Text = "Quick and easy audio device switching with global hotkeys";
                VersionText.Text = "v 1.0.0.0";
            }
        }

        /// <summary>
        /// Handles hyperlink navigation requests
        /// </summary>
        private void OnHyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                // Open URL in default browser
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open link: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handles vendor image click
        /// </summary>
        private void OnVendorImageClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "http://georgousis.info",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open website: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles PayPal logo click
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open PayPal: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        /// <summary>
        /// Handles close button click
        /// </summary>
        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}