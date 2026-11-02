using System;
using System.Globalization;
using System.Windows.Data;

namespace Picture_Sorter.Convertors
{
    /// <summary>
    /// An <see cref="IValueConverter"/> that converts file sizes from bytes to a readable format. (e.g. 1024 -> "1.00 KB")
    /// </summary>
    public sealed class FileSizeConvertor : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Type safty check
            if (value is long sizeBytes)
            {
                // Calculate file sizes in KB and MB
                double sizeKb = sizeBytes / 1024.0;
                double sizeMb = sizeKb / 1024.0;

                // If file size is greater than 1 MB, display in MB, otherwise display in KB.
                return sizeMb >= 1 ? $"{sizeMb:F2} MB" : $"{sizeKb:F2} KB";
            }

            // Fallback value
            return "0 KB";
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
