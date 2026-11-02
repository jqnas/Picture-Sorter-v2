using System;
using System.Media;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

#pragma warning disable CA1416 // Validate platform compatibility
namespace Picture_Sorter.Dialogs
{
    /// <summary>
    /// An image exists dialog that is shown when an image already exists in the destination folder.
    /// </summary>
    /// <remarks>Interaction logic for ImageExistsDialog.xaml</remarks>
    public partial class ImageExistsDialog : Window
    {
        /// <summary>
        /// The new image as an <see cref="ImageModel"/>.
        /// </summary>
        public ImageModel NewImage { get; }

        /// <summary>
        /// The existing image as an <see cref="ImageModel"/>.
        /// </summary>
        public ImageModel ExistingImage { get; }

        /// <summary>
        /// Contains the selected <see cref="Picture_Sorter.ExistingImageAction"/> when the dialog is closed. (Defaults to <see cref="ExistingImageAction.Cancel"/>)
        /// </summary>
        public ExistingImageAction ExistingImageAction { get; private set; } = ExistingImageAction.Cancel;


        /// <summary>
        /// Create a new <see cref="ImageExistsDialog"/>.
        /// </summary>
        /// <param name="newImage">The new image as an <see cref="ImageModel"/>.</param>
        /// <param name="existingImage">The existing image as an <see cref="ImageModel"/>.</param>
        public ImageExistsDialog(ImageModel newImage, ImageModel existingImage)
        {
            // Plays the default message box sound
            SystemSounds.Exclamation.Play();

            // Sets the data context
            this.DataContext = this;

            // Intiailize components
            InitializeComponent();


            // Set the image properties
            NewImage = newImage;
            ExistingImage = existingImage;


            // Trys to load the new image
            try
            {
                // Load new image
                BitmapImage newImageBitmap = new();
                newImageBitmap.BeginInit();
                newImageBitmap.UriSource = new Uri(NewImage.SourcePath, UriKind.Absolute);
                newImageBitmap.CacheOption = BitmapCacheOption.OnLoad; // Prevents file locking
                newImageBitmap.EndInit();

                // Set the new image
                NewImageContainer.Source = newImageBitmap;
            }
            catch { /* Ignore loading errors */ }

            // Trys to load the existing image
            try
            {
                // Load existing image
                BitmapImage existingImageBitmap = new();
                existingImageBitmap.BeginInit();
                existingImageBitmap.UriSource = new Uri(ExistingImage.SourcePath, UriKind.Absolute);
                existingImageBitmap.CacheOption = BitmapCacheOption.OnLoad; // Prevents file locking
                existingImageBitmap.EndInit();

                // Set the existing image
                ExistingImageContainer.Source = existingImageBitmap;
            }
            catch { /* Ignore loading errors */ }

            // Load the image infos of the existing image to display them in the dialog
            existingImage.LoadInfos();

            // Focus the overwrite button (by default)
            OverwriteBtn.Focus();
        }


        /// <summary>
        /// Shows an <see cref="ImageExistsDialog"/>.
        /// </summary>
        /// <param name="newImage">The new image as an <see cref="ImageModel"/>.</param>
        /// <param name="existingImage">The existing image as an <see cref="ImageModel"/>.</param>
        /// <returns>The <see cref="Picture_Sorter.ExistingImageAction"/> to take.</returns>
        public static ExistingImageAction Show(ImageModel newImage, ImageModel existingImage)
        {
            // Creates new existing image dialog
            var infoBox = new ImageExistsDialog(newImage, existingImage);

            // Shows the dialog
            if (infoBox.ShowDialog() == true)
            {
                // Return result
                return infoBox.ExistingImageAction;
            }

            // Invalid
            return ExistingImageAction.Cancel;
        }


        private void OverwriteBtn_Click(object sender, RoutedEventArgs e) => SetResultAndClose(ExistingImageAction.Overwrite);

        private void CancelBtn_Click(object sender, RoutedEventArgs e) => SetResultAndClose(ExistingImageAction.Cancel);

        private void RenameBtn_Click(object sender, RoutedEventArgs e) => SetResultAndClose(ExistingImageAction.Rename);

        /// <summary>
        /// Sets the <see cref="ExistingImageAction"/> and closes the dialog.
        /// </summary>
        /// <param name="action">The <see cref="Picture_Sorter.ExistingImageAction"/> to set.</param>
        private void SetResultAndClose(ExistingImageAction action)
        {
            // Set action and close dialog
            ExistingImageAction = action;
            DialogResult = true;
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Closes the dialog when the escape key is pressed
            if (e.Key == Key.Escape)
            {
                // When closing the dialog with escape, the result is always cancel
                SetResultAndClose(ExistingImageAction.Cancel);
            }
        }
    }
}
#pragma warning restore CA1416 // Validate platform compatibility