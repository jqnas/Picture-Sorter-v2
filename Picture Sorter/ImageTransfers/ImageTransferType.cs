namespace Picture_Sorter.ImageTransfers
{
    /// <summary>
    /// The type of an <see cref="IImageTransfer"/>.
    /// </summary>
    public enum ImageTransferType
    {
        /// <summary>
        /// Copys the image.
        /// </summary>
        Copy,

        /// <summary>
        /// Moves the image.
        /// </summary>
        Move,

        /// <summary>
        /// Deletes the image.
        /// </summary>
        Delete,
    }
}
