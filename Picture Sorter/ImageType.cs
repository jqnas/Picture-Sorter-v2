namespace Picture_Sorter
{
    /// <summary>
    /// The file type of an <see cref="ImageModel"/>
    /// </summary>
    public enum ImageType
    {
        /// <summary>
        /// Unknown file type.
        /// </summary>
        /// <remarks>Do not set this type manually.</remarks>
        UNK = 0,

        /// <summary>
        /// A .jpg image file. (Joint Photographic Experts Group)
        /// </summary>
        JPG,

        /// <summary>
        /// A .jpeg image file. (Joint Photographic Experts Group)
        /// </summary>
        JPEG,

        /// <summary>
        /// A .png image file. (Portable Network Graphics)
        /// </summary>
        PNG,

        /// <summary>
        /// A .tiff image file. (Tagged Image File Format)
        /// </summary>
        TIFF,

        /// <summary>
        /// A .bmp image file. (Bitmap)
        /// </summary>
        BMP,

        /// <summary>
        /// All <see cref="ImageType"/> values exccept <see cref="ImageType.UNK"/>.
        /// </summary>
        /// <remarks>Used for filtering all image types.</remarks>
        All = JPG | JPEG | PNG | TIFF | BMP,
    }
}
