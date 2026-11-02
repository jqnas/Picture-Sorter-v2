using System;
using System.Windows;
using Picture_Sorter.Themes;

namespace Picture_Sorter
{
    /// <summary>
    /// A <see cref="ResourceDictionary"/> that defines the style for the <see cref="MainWindow"/>.
    /// </summary>
    public partial class WindowStyle : ResourceDictionary
    {
        public WindowStyle()
        {
            // Initialize components
            InitializeComponent();
        }


        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            // Closes the application
            Environment.Exit(0);
        }

        private void MaximizeBtn_Click(object sender, RoutedEventArgs e)
        {
            // Get main window
            var window = (Window)((FrameworkElement)sender).TemplatedParent;

            // Toggle window maximize state
            if (window.WindowState == WindowState.Normal)
            {
                window.WindowState = WindowState.Maximized;
            }
            else
            {
                window.WindowState = WindowState.Normal;
            }
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            // Get main window
            var window = (Window)((FrameworkElement)sender).TemplatedParent;

            // Minimize window
            window.WindowState = WindowState.Minimized;
        }

        private void ThemeSwitcherBtn_Click(object sender, RoutedEventArgs e)
        {
            // Get main window
            var window = (MainWindow)((FrameworkElement)sender).TemplatedParent;

            // Switch dark/light mode
            window.Theme = window.Theme == Theme.Dark ? Theme.Light : Theme.Dark;
        }

        private void LoadFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            // Get main window
            var window = (MainWindow)((FrameworkElement)sender).TemplatedParent;

            // Load a new folder
            window.OpenFolder(MainWindow.IsShift, add: MainWindow.IsControl);
        }

        private void ReloadFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            // Get main window
            var window = (MainWindow)((FrameworkElement)sender).TemplatedParent;

            // Reload all current sources
            window.ReloadFolders(refresh: !MainWindow.IsShift, findRaws: MainWindow.IsAlt);
        }

        private void JumpToBtn_Click(object sender, RoutedEventArgs e)
        {
            // Get main window
            var window = (MainWindow)((FrameworkElement)sender).TemplatedParent;

            // Open jump to window
            window.JumpTo();
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            // Get main window
            var window = (MainWindow)((FrameworkElement)sender).TemplatedParent;

            // Toggle search box
            window.ToggleSearchBox();
        }
    }
}
