using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Picture_Sorter.ImageDestinations;

namespace Picture_Sorter.Json
{
    /// <summary>
    /// An <see cref="JsonConverter{T}"/> for <see cref="IImageDestination"/>.
    /// </summary>
    public class ImageDestinationJsonConvertor : JsonConverter<IImageDestination>
    {
        /// <inheritdoc/>
        public override IImageDestination Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // Reads the string value from the JSON
            string path = reader.GetString();

            // Creates a new FolderImageDestination with the read path
            return new FolderImageDestination(path, 0);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, IImageDestination value, JsonSerializerOptions options)
        {
            // When writing, just output the string value of the path
            writer.WriteStringValue(value?.DestPath);
        }
    }
}
