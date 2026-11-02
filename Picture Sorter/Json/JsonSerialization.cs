using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace Picture_Sorter.Json
{
    /// <summary>
    /// A helper class to serialize and deserialize JSON data into files.
    /// </summary>
    public static class JsonSerialization
    {
        // Json options
        private static readonly JsonSerializerOptions jsonSerializerOptions = new()
        {
            AllowTrailingCommas = true,
            WriteIndented = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
        };


        /// <summary>
        /// Saves the provided <paramref name="data"/> to the specified <paramref name="filePath"/>.
        /// </summary>
        /// <typeparam name="T">The type of the data to serialize.</typeparam>
        /// <param name="data">The data to serialize.</param>
        /// <param name="filePath">The file path to save the data to.</param>
        public static void Save<T>(T data, string filePath)
        {
            // Convert to json data
            string jsonString = JsonSerializer.Serialize(data, jsonSerializerOptions);

            // Write to file
            File.WriteAllText(filePath, jsonString);
        }

        /// <summary>
        /// Loads data from the specified <paramref name="filePath"/>.
        /// </summary>
        /// <typeparam name="T">The type of the data to deserialize.</typeparam>
        /// <param name="filePath">The file path to load the data from.</param>
        /// <returns>The deserialized data.</returns>
        public static T Load<T>(string filePath)
        {
            // Read from file
            string jsonString = File.ReadAllText(filePath);

            // Convert from json data
            return JsonSerializer.Deserialize<T>(jsonString, jsonSerializerOptions);
        }
    }
}
