using System;
using System.Globalization;
using System.Windows.Data;

namespace Picture_Sorter.Convertors
{
    /// <summary>
    /// An <see cref="IValueConverter"/> that inverts boolean values.
    /// </summary>
    public sealed class InverseBoolConvertor : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Type safty check
            if (value is bool b)
            {
                // Inverts True to False, and False to True
                return !b; 
            }

            // Fallback value
            return true;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
