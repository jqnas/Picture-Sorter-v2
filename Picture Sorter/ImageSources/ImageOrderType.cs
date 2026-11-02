namespace Picture_Sorter.ImageSources
{
    /// <summary>
    /// The order type of images.
    /// </summary>
    public enum ImageOrderType
    {
        /// <summary>
        /// Order by the image's last write time.
        /// </summary>
        LastWriteAsc,

        /// <summary>
        /// Order by the image's last write time. (descending)
        /// </summary>
        LastWriteDesc,

        /// <summary>
        /// Order by the image's creation time.
        /// </summary>
        CreationTimeAsc,

        /// <summary>
        /// Order by the image's creation time. (descending)
        /// </summary>
        CreationTimeDesc,

        /// <summary>
        /// Order by the image's size.
        /// </summary>
        SizeAsc,

        /// <summary>
        /// Order by the image's size. (descending)
        /// </summary>
        SizeDesc,

        /// <summary>
        /// Order by the image's name.
        /// </summary>
        NameAsc,

        /// <summary>
        /// Order by the image's name. (descending)
        /// </summary>
        NameDesc,

        /// <summary>
        /// Order by the folder name that contains the image.
        /// </summary>
        DirectoryAsc,

        /// <summary>
        /// Order by the folder name that contains the image. (descending)
        /// </summary>
        DirectoryDesc,
    }
}
