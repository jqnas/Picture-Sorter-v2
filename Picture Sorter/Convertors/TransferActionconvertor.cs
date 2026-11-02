using System;
using System.Globalization;
using System.Windows.Data;
using Picture_Sorter.ImageTransfers;

namespace Picture_Sorter.Convertors
{
    /// <summary>
    /// An <see cref="IValueConverter"/> that converts an <see cref="ImageTransferType"/> to a string-representation. (e.g. "Copied", "Moved", "Deleted")
    /// </summary>
    public sealed class TransferActionconvertor : IValueConverter
    {
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Type safty check
            if (value is ImageTransferType transferType)
            {
                // Return the string representation of the transfer type
                return transferType switch
                {
                    ImageTransferType.Copy => " Copied ",
                    ImageTransferType.Move => " Moved",
                    ImageTransferType.Delete => " Deleted",
                    _ => throw new NotImplementedException($"The transfer type '{transferType}' is not implemented."),
                };
            }

            // Fallback value
            return "Transfered";
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
