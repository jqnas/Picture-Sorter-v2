using System.ComponentModel;
using System.IO;

namespace Picture_Sorter.ImageDestinations
{
    /// <summary>
    /// Defines a folder as an <see cref="IImageDestination"/>.
    /// </summary>
    public sealed class FolderImageDestination : IImageDestination
    {
        /// <inheritdoc/>
        public string DestPath
        { 
            get;
            set
            {
                field = value;

                // Update properties that depend on DestPath
                OnPropertyChanged(nameof(DestPath));
                OnPropertyChanged(nameof(UIDestPath));
            }
        }

        /// <inheritdoc/>
        public int Number
        {
            get;
            set
            {
                field = value;

                // Update properties that depend on Numbe
                OnPropertyChanged(nameof(Number));
                OnPropertyChanged(nameof(UINumber));
            }
        }

        /// <inheritdoc/>
        public int Counter
        {
            get;
            set
            {
                field = value;

                // Update properties that depend on Counter
                OnPropertyChanged(nameof(Counter));
            }
        } = 0;

        /// <inheritdoc/>
        public string UIDestPath => new DirectoryInfo(DestPath).Name;

        /// <inheritdoc/>
        public string UINumber => $"{Number}:";


        /// <summary>
        /// Creates a new <see cref="FolderImageDestination"/>.
        /// </summary>
        /// <param name="path">The path to the folder.</param>
        /// <param name="number">The number of the destination.</param>
        public FolderImageDestination(string path, int number)
        {
            DestPath = path;
            Number = number;
        }


        // PropertyChanged event and method to raise the event and track changes in properties
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
