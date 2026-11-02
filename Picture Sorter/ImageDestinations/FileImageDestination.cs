using System.ComponentModel;
using System.IO;

namespace Picture_Sorter.ImageDestinations
{
    /// <summary>
    /// Defines a file as an <see cref="IImageDestination"/>.
    /// </summary>
    public sealed class FileImageDestination : IImageDestination
    {
        /// <summary>
        /// The new file name and path.
        /// </summary>
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

                // Update properties that depend on Number
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
        public string UIDestPath => new DirectoryInfo(DestPath).Parent?.Name;

        /// <inheritdoc/>
        public string UINumber => $"{Number}:";


        /// <summary>
        /// Creates a new <see cref="FileImageDestination"/>.
        /// </summary>
        /// <param name="path">The new file name and path.</param>
        /// <param name="number">The number of the destination.</param>
        public FileImageDestination(string path, int number)
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
