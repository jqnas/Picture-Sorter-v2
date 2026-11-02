using System.ComponentModel;
using System.Text.Json.Serialization;
using Picture_Sorter.Json;

namespace Picture_Sorter.ImageDestinations
{
    /// <summary>
    /// Defines an image destination.
    /// </summary>
    [JsonConverter(typeof(ImageDestinationJsonConvertor))]
    public interface IImageDestination : INotifyPropertyChanged
    {
        /// <summary>
        /// The path of the destination.
        /// </summary>
        public string DestPath { get; set; }

        /// <summary>
        /// The number of the destination to display in the UI.
        /// </summary>
        public int Number { get; set; }

        /// <summary>
        /// The count of items transfered to the destination.
        /// </summary>
        public int Counter { get; set; }

        /// <summary>
        /// The path of the destination to display in the UI.
        /// </summary>
        public string UIDestPath { get;}

        /// <summary>
        /// Gets the number to display in the UI.
        /// </summary>
        public string UINumber { get; }
    }
}
