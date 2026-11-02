namespace Picture_Sorter
{
    /// <summary>
    /// Defines an action, if a physical image file already exists.
    /// </summary>
    public enum ExistingImageAction
    {
        /// <summary>
        /// Cancel the operation.
        /// </summary>
        Cancel = 0,

        /// <summary>
        /// Overwrite the existing file with the new file.
        /// </summary>
        Overwrite = 1,

        /// <summary>
        /// Rename the new file.
        /// </summary>
        Rename = 2,
    }
}
