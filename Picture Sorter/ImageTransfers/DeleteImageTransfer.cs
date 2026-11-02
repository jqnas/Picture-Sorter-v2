using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Windows;
using Picture_Sorter.Dialogs;

namespace Picture_Sorter.ImageTransfers
{
    /// <summary>
    /// Defines a move delete action.
    /// </summary>
    public sealed class DeleteImageTransfer : IImageTransfer
    {
        /// <inheritdoc/>
        public ImageModel Image { get; }

        /// <inheritdoc/>
        public ImageTransferType TransferType { get; } = ImageTransferType.Delete;

        /// <summary>
        /// Indicates whether recycling is disabled.
        /// </summary>
        public bool DontRecycle
        {
            get;
            private set
            {
                field = value;

                OnPropertyChanged(nameof(DontRecycle));
            }
        } = false;

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
        public bool TransferedRaw
        {
            get;
            private set
            {
                field = value;

                OnPropertyChanged(nameof(TransferedRaw));
            }
        } = false;

        public string RawDestFile => DestFile;

        /// <inheritdoc/>
        public string UISourceFile => Path.GetFileName(Image.SourcePath);

        /// <inheritdoc/>
        public string UIDestFolder => DestFile;


        /// <summary>
        /// Creates a new <see cref="DeleteImageTransfer"/>.
        /// </summary>
        /// <param name="image">The image to delete</param>
        /// <param name="dontRecycle">Indicates wether to disable recycling.</param>
        public DeleteImageTransfer(ImageModel image, bool dontRecycle = false)
        {
            Image = image;
            DestFile = dontRecycle ? "-" : "Trash";
            DontRecycle = dontRecycle;
        }


        // PropertyChanged event and method to raise the event and track changes in properties
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        /// <summary>
        /// Deletes the <see cref="Image"/> or moves it to the trash.
        /// </summary>
        /// <returns><see langword="true"/> if the file was successfully deleted; otherwise <see langword="false"/>.</returns>
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
                var rawResult = InformationBoxDialog.Show("Raw file not found", $"Raw file '{Image.RawSourcePath}' does not exists. Do you want to unlink the raw file?", MessageBoxButton.YesNoCancel);

                if (rawResult == MessageBoxResult.Yes)
                {
                    // Removes the raw path from the image
                    Image.RawSourcePath = null;
                }
                else if (rawResult == MessageBoxResult.No) { return false; }
                else if (rawResult == MessageBoxResult.Cancel) { return false; }
                else { return false; }
            }

            // Prompt a confirmation
            var result = InformationBoxDialog.Show("Delete image", $"Do you want to{(DontRecycle ? " permanantly" : string.Empty)} delete '{Image.SourcePath}{(Image.HasRaw ? $" and the linked raw file" : string.Empty)}'?", MessageBoxButton.YesNoCancel);

            // If no explicitly confirmed overwrite, cancel
            if (result != MessageBoxResult.Yes) { return false; }

            try
            {
                if (DontRecycle)
                {
                    // Delete file
                    File.Delete(Image.SourcePath);

                    // Deletes the raw file
                    if (Image.HasRaw)
                    {
                        File.Delete(Image.RawSourcePath);

                        // Mark raw as transfered
                        TransferedRaw = true;
                    }
                }
                else
                {
                    // Move file to trash
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(Image.SourcePath, Microsoft.VisualBasic.FileIO.UIOption.AllDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin, Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);

                    // Deletes the raw file
                    if (Image.HasRaw)
                    {
                        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(Image.RawSourcePath, Microsoft.VisualBasic.FileIO.UIOption.AllDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin, Microsoft.VisualBasic.FileIO.UICancelOption.ThrowException);

                        // Mark raw as transfered
                        TransferedRaw = true;
                    }
                }

                // Mark as deleted
                Image.DeleteCount++;
            }
            catch 
            {
                InformationBoxDialog.Show("Error", $"Unable to delete image {(Image.HasRaw ? $" or raw linked file" : string.Empty)} '{Image.SourcePath}'.", MessageBoxButton.OK);
                return false; 
            }

            return true;
        }

        /// <inheritdoc/>
        public bool Undo()
        {
            if (DontRecycle)
            {
                InformationBoxDialog.Show("Undo deletion", $"The image '{Image.SourcePath}' {(TransferedRaw ? $"and linked raw file were" : "was")} deleted permanantly. Undoing is not possible", MessageBoxButton.OK);
            }
            else
            {
                var result = InformationBoxDialog.Show("Undo deletion", $"The image '{Image.SourcePath}'{(TransferedRaw ? $" and linked raw file" : string.Empty)} can be recovered uisng the operating system's trash. Open trash?", MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Yes)
                {
                    // Opens the Windows Recycle Bin window
                    Process.Start(new ProcessStartInfo("explorer.exe", "shell:RecycleBinFolder") { UseShellExecute = true });
                }
                else if (result == MessageBoxResult.No) { return false; }
                else if (result == MessageBoxResult.Cancel) { return false; }
                else { return false; }
            }

            return false;
        }
    }
}
