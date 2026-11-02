using System;
using System.Globalization;
using System.Windows.Data;

namespace Picture_Sorter.Convertors
{
    /// <summary>
    /// An <see cref="IValueConverter"/> that converts an 0-based index values to 1-based number values. (e.g. 0 -> 1., 1 -> 2., ...)
    /// </summary>
    public sealed class IndexToNumberConvertor : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Type safty check
            if (value is int index)
            {
                // If nothing is selected, SelectedIndex is -1.
                // Return a placeholder '-'
                if (index < 0) { return "-"; }

                // Convert 0-based index to 1-based numbering
                return (index + 1).ToString() + '.';
            }

            // Fallback value
            return "-";
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
