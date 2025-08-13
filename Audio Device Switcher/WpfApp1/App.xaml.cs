using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace AudioDeviceSwitcher
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// Enhanced with system tray startup logic
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Reference to the main window instance for tray operations
        /// </summary>
        private MainWindow mainWindowInstance;

        /// <summary>
        /// Handles application startup event
        /// Implements start-in-tray functionality based on settings
        /// </summary>
        protected override void OnStartup(StartupEventArgs startupEventArgs)
        {
            try
            {
                // Call base startup logic first
                base.OnStartup(startupEventArgs);

                // Set the main window as the application's main window
                // This is important for proper shutdown behavior
                this.MainWindow = new MainWindow();

                // Check if application should start in tray mode
                bool shouldStartInTray = (this.MainWindow as MainWindow)?.ShouldStartInTray() ?? false;
                if (shouldStartInTray)
                {
                    // Start minimized to system tray - don't show window
                    System.Diagnostics.Debug.WriteLine("Application started in system tray mode");
                }
                else
                {
                    // Normal startup - show the main window
                    this.MainWindow.Show();
                    System.Diagnostics.Debug.WriteLine("Application started with main window visible");
                }

                mainWindowInstance = this.MainWindow as MainWindow;
            }
            catch (Exception startupException)
            {
                // Handle any startup errors gracefully
                System.Diagnostics.Debug.WriteLine($"Error during application startup: {startupException.Message}");

                // Show error message to user
                MessageBox.Show(
                    $"An error occurred during application startup:\n\n{startupException.Message}\n\nThe application will attempt to continue with default settings.",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // Try to continue with a basic main window
                try
                {
                    if (mainWindowInstance == null)
                    {
                        mainWindowInstance = new MainWindow();
                        this.MainWindow = mainWindowInstance;
                    }

                    // Always show window if there was a startup error
                    mainWindowInstance.Show();
                }
                catch (Exception fallbackException)
                {
                    // If even the fallback fails, show critical error and shutdown
                    System.Diagnostics.Debug.WriteLine($"Critical startup error: {fallbackException.Message}");
                    MessageBox.Show(
                        $"Critical error: Unable to start the application.\n\n{fallbackException.Message}",
                        "Critical Startup Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    // Shutdown the application
                    this.Shutdown(1);
                }
            }
        }

        /// <summary>
        /// Handles application exit event
        /// Ensures proper cleanup of system tray resources
        /// </summary>
        protected override void OnExit(ExitEventArgs exitEventArgs)
        {
            try
            {
                // Ensure main window is properly disposed
                mainWindowInstance?.Dispose();
                System.Diagnostics.Debug.WriteLine("Application cleanup completed successfully");
            }
            catch (Exception exitException)
            {
                // Log cleanup errors but don't prevent application exit
                System.Diagnostics.Debug.WriteLine($"Error during application exit cleanup: {exitException.Message}");
            }
            finally
            {
                // Always call base exit logic
                base.OnExit(exitEventArgs);
            }
        }

        /// <summary>
        /// Handles unhandled exceptions in the application
        /// Provides graceful error handling and user notification
        /// </summary>
        private void OnApplicationUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs unhandledExceptionArgs)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Unhandled application exception: {unhandledExceptionArgs.Exception}");

                // Show user-friendly error message
                string errorMessage = $"An unexpected error occurred:\n\n{unhandledExceptionArgs.Exception.Message}\n\nThe application will continue running, but some features may not work correctly.";

                MessageBox.Show(
                    errorMessage,
                    "Unexpected Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                // Mark exception as handled to prevent application crash
                unhandledExceptionArgs.Handled = true;
            }
            catch (Exception handlerException)
            {
                // If error handling itself fails, log it but don't interfere with shutdown
                System.Diagnostics.Debug.WriteLine($"Error in exception handler: {handlerException.Message}");
            }
        }

        /// <summary>
        /// Application constructor - sets up global exception handling
        /// </summary>
        public App()
        {
            try
            {
                // Subscribe to unhandled exception events for graceful error handling
                this.DispatcherUnhandledException += OnApplicationUnhandledException;

                // Also handle exceptions in other threads
                AppDomain.CurrentDomain.UnhandledException += OnDomainUnhandledException;

                System.Diagnostics.Debug.WriteLine("Application exception handlers configured");
            }
            catch (Exception constructorException)
            {
                // Log constructor errors but allow application to continue
                System.Diagnostics.Debug.WriteLine($"Error in App constructor: {constructorException.Message}");
            }
        }

        /// <summary>
        /// Handles unhandled exceptions in non-UI threads
        /// </summary>
        private void OnDomainUnhandledException(object sender, UnhandledExceptionEventArgs domainExceptionArgs)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Unhandled domain exception: {domainExceptionArgs.ExceptionObject}");

                // For non-UI thread exceptions, we can only log them
                // Cannot show MessageBox from non-UI threads safely
            }
            catch (Exception handlerException)
            {
                // If error handling itself fails, log it
                System.Diagnostics.Debug.WriteLine($"Error in domain exception handler: {handlerException.Message}");
            }
        }
    }
}