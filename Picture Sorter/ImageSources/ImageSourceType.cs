namespace Picture_Sorter.ImageSources
{
    /// <summary>
    /// Defines the type of a <see cref="IImageSource"/>.
    /// </summary>
    public enum ImageSourceType
    {
        /// <summary>
        /// An image file.
        /// </summary>
        File,

        /// <summary>
        /// A folder containing images.
        /// </summary>
        Folder,
    }
}
