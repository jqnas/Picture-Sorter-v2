using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Picture_Sorter.ImageSources;

namespace Picture_Sorter.Dialogs
{
    /// <summary>
    /// An loading source options dialog that allows the user to select options for loading image sources.
    /// </summary>
    /// <remarks>Interaction logic for LoadingSourceOptionsDialog.xaml</remarks>
    public partial class LoadingSourceOptionsDialog : Window, INotifyPropertyChanged
    {
        /// <summary>
        /// All available <see cref="Picture_Sorter.ImageType"/> values, except <see cref="ImageType.Unknown"/>.
        /// </summary>
        public IEnumerable<ImageType> ImageTypes { get; } = Enum.GetValues<ImageType>().Skip(1);

        /// <summary>
        /// All available <see cref="ImageSources.ImageOrderType"/> values.
        /// </summary>
        public IEnumerable<ImageOrderType> ImageOrderTypes { get; } = Enum.GetValues<ImageOrderType>();

        /// <summary>
        /// Contains the <see cref="MessageBoxResult"/> of the dialog on close. (Defaults to <see cref="MessageBoxResult.Cancel"/>)
        /// </summary>
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.Cancel;

        /// <summary>
        /// Indicates wether to load the folder recursively.
        /// </summary>
        public bool LoadRecursive
        {
            get;
            set
            {
                field = value;

                // Update property changed
                OnPropertyChanged(nameof(LoadRecursive));
            }
        } = false;

        /// <summary>
        /// Indicates wether to add the loaded sources.
        /// </summary>
        public bool AddSources
        {
            get;
            set
            {
                field = value;

                // Update property changed
                OnPropertyChanged(nameof(AddSources));
            }
        } = true;

        /// <summary>
        /// Indicates wether to search for raw files when loading sources.
        /// </summary>
        public bool FindRaws
        {
            get;
            set
            {
                field = value;

                // Update property changed
                OnPropertyChanged(nameof(FindRaws));
            }
        } = false;

        /// <summary>
        /// The <see cref="Picture_Sorter.ImageType"/> to select when loading sources. (Defaults to <see cref="ImageType.All"/>)
        /// </summary>
        public ImageType ImageType
        {
            get;
            set
            {
                field = value;

                // Update property changed
                OnPropertyChanged(nameof(ImageType));
            }
        } = ImageType.All;

        /// <summary>
        /// The <see cref="ImageSources.ImageOrderType"/> to use when loading sources. (Defaults to <see cref="ImageOrderType.LastWriteAsc"/>)
        /// </summary>
        public ImageOrderType ImageOrderType
        {
            get;
            set
            {
                field = value;

                // Update property changed
                OnPropertyChanged(nameof(ImageOrderType));
            }
        } = ImageOrderType.LastWriteAsc;


        /// <summary>
        /// Creates a new <see cref="LoadingSourceOptionsDialog"/>.
        /// </summary>
        /// <param name="loadRecursive">A value indicating whether to load the folder recursive.</param>
        /// <param name="addSources">A value indicating whether to add the loaded sources.</param>
        /// <param name="findRaws">A value indicating whether to search for raw files when loading sources.</param>
        public LoadingSourceOptionsDialog(bool loadRecursive, bool addSources, bool findRaws)
        {
            // Sets the data context
            this.DataContext = this;

            // Initilize components
            InitializeComponent();


            // Set default options
            LoadRecursive = loadRecursive;
            AddSources = addSources;
            FindRaws = findRaws;


            // Sets focus to the OK button
            OkBtn.Focus();
        }


        // PropertyChanged event and method to raise the event and track changes in properties
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        /// <summary>
        /// Show a <see cref="LoadingSourceOptionsDialog"/>.
        /// </summary>
        /// <param name="loadRecursive">A value indicating whether to load the folder recursive.</param>
        /// <param name="addSources">A value indicating whether to add the loaded sources.</param>
        /// <param name="findRaws">A value indicating whether to search for raw files when loading sources.</param>
        /// <param name="imageType">The <see cref="Picture_Sorter.ImageType"/> of images to load.</param>
        /// <param name="orderType">The <see cref="ImageSources.ImageOrderType"/> in which to load the images.</param>
        /// <returns>The <see cref="MessageBoxResult"/> of the dialog.</returns>
        public static MessageBoxResult Show(ref bool loadRecursive, ref bool addSources, ref bool findRaws, ref ImageType imageType, ref ImageOrderType orderType)
        {
            // Creates a new loading source options dialog
            var openFolderOptinsDialog = new LoadingSourceOptionsDialog(loadRecursive, addSources, findRaws);

            // Shows the dialog
            if (openFolderOptinsDialog.ShowDialog() == true)
            {
                // Set and return result
                loadRecursive = openFolderOptinsDialog.LoadRecursive;
                addSources = openFolderOptinsDialog.AddSources;
                findRaws = openFolderOptinsDialog.FindRaws;
                imageType = openFolderOptinsDialog.ImageType;
                orderType = openFolderOptinsDialog.ImageOrderType;
                return openFolderOptinsDialog.Result;
            }

            // Invalid
            return MessageBoxResult.Abort;
        }


        private void OkBtn_Click(object sender, RoutedEventArgs e) => SetResultAndClose(MessageBoxResult.OK);

        private void CancelBtn_Click(object sender, RoutedEventArgs e) => SetResultAndClose(MessageBoxResult.Cancel);

        /// <summary>
        /// Sets the <see cref="MessageBoxResult"/> and closes the dialog.
        /// </summary>
        /// <param name="result">The <see cref="MessageBoxResult"/> to set.</param>
        private void SetResultAndClose(MessageBoxResult result)
        {
            // Set result and close dialog
            Result = result;
            DialogResult = true;
            Close();
        }

        private void CheckBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Toggles the checkbox when the enter or space key is pressed on it
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                // Gets the associated checkbox
                var checkBox = sender as CheckBox;

                // Toggles the checkbox
                checkBox.IsChecked = !checkBox.IsChecked;

                // Mark event as handled
                e.Handled = true;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Closes the dialog when the escape key is pressed
            if (e.Key == Key.Escape)
            {
                // When closing the dialog with escape, the result is always cancel
                SetResultAndClose(MessageBoxResult.Cancel);
            }
        }

        private void ComboBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Toggles the combobox dropdown when the enter or space key is pressed on it
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                // Gets the associated combobox
                var combobox = sender as ComboBox;

                // Toggles the combobox dropdown
                combobox.IsDropDownOpen = !combobox.IsDropDownOpen;

                // Mark event as handled
                e.Handled = true;
            }
        }
    }
}
