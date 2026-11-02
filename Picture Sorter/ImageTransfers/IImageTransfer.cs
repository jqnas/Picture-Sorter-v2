using System.ComponentModel;
using System.IO;

namespace Picture_Sorter.ImageTransfers
{
    /// <summary>
    /// Describes a file transfer action.
    /// </summary>
    public interface IImageTransfer : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets the transfered <see cref="ImageModel"/>.
        /// </summary>
        public ImageModel Image { get; }

        /// <summary>
        /// The <see cref="ImageTransferType"/> of the transfer action.
        /// </summary>
        public ImageTransferType TransferType { get; }

        /// <summary>
        /// Gets the new physical file path.
        /// </summary>
        public string DestFile { get; }

        /// <summary>
        /// Gets the new physical raw file path.
        /// </summary>
        public string RawDestFile { get; }

        /// <summary>
        /// Indicates wether the image's raw file has been transfered, if available.
        /// </summary>
        public bool TransferedRaw { get; }

        /// <summary>
        /// Gets the file name to display in the UI.
        /// </summary>
        public string UISourceFile { get; }

        /// <summary>
        /// Gets the directory name of the destination to display in the UI.
        /// </summary>
        public string UIDestFolder { get; }


        /// <summary>
        /// Transfers the <see cref="Image"/> to <see cref="DestFile"/>.
        /// </summary>
        /// <returns><see langword="true"/> if the file was successfully transfered; otherwise <see langword="false"/>.</returns>
        public bool Transfer();

        /// <summary>
        /// Undo the file transfer action.
        /// </summary>
        /// <returns><see langword="true"/> if the transfer was undone successfully; otherwise <see langword="false"/>.</returns>
        public bool Undo();


        /// <summary>
        /// Trys to get a unique file name based on the given <paramref name="filePath"/>.
        /// </summary>
        /// <remarks>If a file with the same name already exists, it appends a number in parentheses to the file name until a unique name is found
        /// or until 1000 attempts have been made.</remarks>
        /// <param name="filePath"></param>
        /// <param name="uniqueFileName"></param>
        /// <returns></returns>
        protected static bool TryGetUniqueFileName(string filePath, out string uniqueFileName)
        {
            // Extract the directory, file name (without extension), and extension
            string directory = Path.GetDirectoryName(filePath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            // Counter variable
            int counter = 1;

            // Variable to hold the new file path
            string newFilePath;

            do
            {
                // Build the new file name with the counter
                newFilePath = Path.Combine(directory, $"{fileNameWithoutExtension} ({counter}){extension}");
                
                // Increase the counter
                counter++;
            }
            while (File.Exists(newFilePath) && counter < 1000); // Prevent infinite loop

            // If we found a unique name, return it
            if (counter < 1000)
            {
                uniqueFileName = newFilePath;
                return true;
            }

            // Return null
            uniqueFileName = null;
            return false;
        }
    }
}
