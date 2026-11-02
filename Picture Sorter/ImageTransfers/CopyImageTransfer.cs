using System.ComponentModel;
using System.IO;
using System.Windows;
using Picture_Sorter.Dialogs;
using Picture_Sorter.ImageDestinations;

namespace Picture_Sorter.ImageTransfers
{
    /// <summary>
    /// Defines a copy file action.
    /// </summary>
    public sealed class CopyImageTransfer : IImageTransfer
    {
        /// <inheritdoc/>
        public ImageModel Image { get; }

        /// <summary>
        /// Gets the <see cref="IImageDestination"/>.
        /// </summary>
        public IImageDestination Destination { get; }

        /// <inheritdoc/>
        public ImageTransferType TransferType { get; } = ImageTransferType.Copy;

        /// <inheritdoc/>
        public string DestFile
        { 
            get; 
            private set
            {
                field = value;

                OnPropertyChanged(nameof(DestFile));
                OnPropertyChanged(nameof(UIDestFolder));
            }
        }

        /// <inheritdoc/>
        public string RawDestFile
        {
            get;
            private set
            {
                field = value;

                OnPropertyChanged(nameof(RawDestFile));
            }
        }

        /// <inheritdoc/>
        public bool TransferedRaw
        {
            get;
            private set
            {
                field = value;

                OnPropertyChanged(nameof(TransferedRaw));
            }
        } = false;

        /// <inheritdoc/>
        public string UISourceFile => Path.GetFileName(Image.SourcePath);

        /// <inheritdoc/>
        public string UIDestFolder => new DirectoryInfo(Path.GetDirectoryName(DestFile)).Name;


        /// <summary>
        /// Creates a new <see cref="CopyImageTransfer"/>.
        /// </summary>
        /// <param name="image">The image to copy represented by an <see cref="ImageModel"/>.</param>
        /// <param name="destination">The <see cref="IImageDestination"/> to copy the image to.</param>
        /// <param name="includesNewName">Indicates wether <paramref name="destination"/> includes a new fileName.</param>
        public CopyImageTransfer(ImageModel image, IImageDestination destination, bool includesNewName = false)
        {
            Image = image;
            Destination = destination;
            
            // Creates the new image name and raw file name
            DestFile = includesNewName ? Destination.DestPath : Path.Combine(Destination.DestPath, Path.GetFileName(Image.SourcePath));
            
            if (Image.HasRaw)
            {
                RawDestFile = includesNewName
                    ? Path.Combine(Path.GetDirectoryName(Destination.DestPath), Path.GetFileNameWithoutExtension(Destination.DestPath)) + Path.GetExtension(Image.RawSourcePath)
                    : Path.Combine(Destination.DestPath, Path.GetFileName(Image.RawSourcePath));
            }
        }


        // PropertyChanged event and method to raise the event and track changes in properties
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        /// <summary>
        /// Copys the <see cref="Image"/> to <see cref="DestFile"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the file was successfully copyied; otherwise <see langword="false"/>.</returns>
        public bool Transfer()
        {
            // Checks if the source image is available
            if (!File.Exists(Image.SourcePath)) 
            {
                InformationBoxDialog.Show("File not found", $"Source file '{Image.SourcePath}' does not exists.", MessageBoxButton.OK);
                return false;
            }
            // Checks if the raw file is available, if the image has a raw file
            else if (Image.HasRaw && !File.Exists(Image.RawSourcePath))
            {
                // Prompts the user to choose an action if the raw file is missing
                var result = InformationBoxDialog.Show("Raw file not found", $"Raw file '{Image.RawSourcePath}' does not exists. Do you want to unlink the raw file?", MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Yes) 
                { 
                    // Removes the raw path from the image
                    Image.RawSourcePath = null;
                }
                else if (result == MessageBoxResult.No) { return false; }
                else if (result == MessageBoxResult.Cancel) { return false; }
                else { return false; }
            }


            // Checks if a image with the same name already exists on the destination
            if (File.Exists(DestFile))
            {
                // Prompts the user to choose an action if a file with the same name already exists
                var result = ImageExistsDialog.Show(Image, new(new(DestFile), 0));

                if (result == ExistingImageAction.Cancel) { return false; }
                else if (result == ExistingImageAction.Rename) 
                {
                    // Trys to create a unique file name for the destination file
                    if (!IImageTransfer.TryGetUniqueFileName(DestFile, out var uniqueFileName))
                    {
                        InformationBoxDialog.Show("Error", $"Unable to create a unique file name for '{DestFile}'.", MessageBoxButton.OK);
                        return false;
                    }

                    // Sets the new file name
                    DestFile = uniqueFileName;

                    // Changes the raw file name to match the new image name
                    RawDestFile = Path.ChangeExtension(DestFile, Path.GetExtension(Image.RawSourcePath));
                }
                else if (result == ExistingImageAction.Overwrite) { /* just overwrite */ }
                else { return false; }
            }

            // Checks if a raw file with the same name already exists on the destination
            if (Image.HasRaw && File.Exists(RawDestFile))
            {
                // Prompts the user to choose an action if a raw file with the same name already exists
                var result = InformationBoxDialog.Show("Raw file does already exists", $"A raw file with the name '{Path.GetFileName(RawDestFile)}' does already exists in '{new DirectoryInfo(RawDestFile).Parent?.Name}'. Do you want to override it?", MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Yes) { /* just overwrite */ }
                else if (result == MessageBoxResult.No) { return false; }
                else if (result == MessageBoxResult.Cancel) { return false; }
                else { return false; }
            }


            try
            {
                // Copys the image
                File.Copy(Image.SourcePath, DestFile, true);

                // Copys the raw file
                if (Image.HasRaw)
                {
                    File.Copy(Image.RawSourcePath, RawDestFile, true);

                    // Mark raw as transfered
                    TransferedRaw = true;
                }

                // Mark as copied
                Image.CopyCount++;

                // Updates the counter
                Destination.Counter++;
            }
            catch 
            {
                InformationBoxDialog.Show("Error", $"Unable to copy image{(Image.HasRaw ? " or raw linked raw file" : string.Empty)} from '{Image.SourcePath}' to '{DestFile}'.", MessageBoxButton.OK);
                return false; 
            }

            return true;
        }

        /// <inheritdoc/>
        public bool Undo()
        {
            // If the destination file (or raw file) doesn't exists anymore, undo is not possible
            if (!File.Exists(DestFile))
            {
                InformationBoxDialog.Show("Undo failed", $"File not found '{DestFile}'.", MessageBoxButton.OK);
                return false;
            }
            else if (TransferedRaw && !File.Exists(RawDestFile))
            {
                InformationBoxDialog.Show("Undo failed", $"Raw file not found '{RawDestFile}'.", MessageBoxButton.OK);
                return false;
            }


            try
            {
                // If the Source file got removed, restore it
                if (!File.Exists(Image.SourcePath))
                {
                    File.Move(DestFile, Image.SourcePath, true);
                }
                // Remove the destination file, since it only got copied
                else
                {
                    File.Delete(DestFile);
                }

                if (TransferedRaw)
                {
                    // If the Raw file got removed, restore it
                    if (!File.Exists(Image.RawSourcePath))
                    {
                        File.Move(RawDestFile, Image.RawSourcePath, true);
                    }
                    // Remove the destination raw file, since it only got copied
                    else
                    {
                        File.Delete(RawDestFile);
                    }
                }
                

                // Resets the status
                Image.CopyCount--;

                // Updates the counter
                Destination.Counter--;
            }
            catch 
            {
                InformationBoxDialog.Show("Error", $"Unable to move image{(Image.HasRaw ? " or linked raw file" : string.Empty)} back to '{Image.SourcePath}' or delete '{DestFile}'.", MessageBoxButton.OK);
                return false; 
            }

            return true;
        }
    }
}
