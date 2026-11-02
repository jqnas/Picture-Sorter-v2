using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Picture_Sorter.ImageDestinations.Templates
{
    /// <summary>
    /// A destination template.
    /// </summary>
    public sealed class DestinationTemplate : INotifyPropertyChanged
    {
        /// <summary>
        /// The name of the template.
        /// </summary>
        public string Name 
        {
            get; 
            set
            {
                // If this is the default template, the name cannot be changed
                if (IsDefault) { return; }

                field = value;

                // Update property change
                OnPropertyChanged(nameof(Name));
            }
        }

        /// <summary>
        /// Indicates wether this is the default <see cref="DestinationTemplate"/>. The default template cannot renamed."/>
        /// </summary>
        [JsonIgnore]
        public bool IsDefault { get; init; }

        /// <summary>
        /// The collection of <see cref="IImageDestination"/>s that belong to this template.
        /// </summary>
        public ObservableCollection<IImageDestination> Destinations { get; init; } = [];


        /// <summary>
        /// Creates a new <see cref="DestinationTemplate"/>.
        /// </summary>
        /// <param name="name">The name of the template.</param>
        public DestinationTemplate(string name)
        {
            Name = name;
        }


        // PropertyChanged event and method to raise the event and track changes in properties
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
