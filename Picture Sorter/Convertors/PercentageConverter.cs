using System;
using System.Globalization;
using System.Windows.Data;

namespace Picture_Sorter.Convertors
{
    /// <summary>
    /// An <see cref="IValueConverter"/> that converts double values to percentage strings. (e.g. 0.01 -> "1%", 0.8 -> "80%", ...)
    /// </summary>
    public sealed class PercentageConverter : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Type safty check
            if (value is double d)
            {
                // Converts a double value to a percentage value
                double percentage = d * 100;

                // Formats the percentage value to a string with no decimal places and adds a percentage sign
                return $"{percentage:F0}%";
            }

            // Fallback value
            return "-";
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
