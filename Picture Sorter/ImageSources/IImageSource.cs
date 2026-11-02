using System;
using System.Collections.Generic;
using System.IO;

namespace Picture_Sorter.ImageSources
{
    /// <summary>
    /// Defines an image source
    /// </summary>
    public interface IImageSource
    {
        /// <summary>
        /// The path to the image source.
        /// </summary>
        public string SourcePath { get; }

        /// <summary>
        /// The <see cref="ImageSourceType"/> of this source.
        /// </summary>
        public ImageSourceType SourceType { get; }

        /// <summary>
        /// The <see cref="Picture_Sorter.ImageType"/> that should be loaded.
        /// </summary>
        public ImageType ImageType { get; }


        /// <summary>
        /// Indicates wether the source is a <see cref="ImageSourceType.File"/>.
        /// </summary>
        public bool IsFile => SourceType == ImageSourceType.File;

        /// <summary>
        /// Indicates wether the source is a <see cref="ImageSourceType.Folder"/>.
        /// </summary>
        public bool IsFolder => SourceType == ImageSourceType.Folder;


        /// <summary>
        /// Loads all images from this source.
        /// </summary>
        /// <returns>A collection of image files represented by a <see cref="FileInfo"/> object.</returns>
        public IEnumerable<FileInfo> Load();

        /// <summary>
        /// Loads all raw images from this source.
        /// </summary>
        /// <returns>A collection of image files represented by a <see cref="FileInfo"/> object.</returns>
        public IEnumerable<FileInfo> LoadRaws();


        /// <summary>
        /// Gets the file extension for a given <paramref name="imageType"/>.
        /// </summary>
        /// <param name="imageType">The <see cref="Picture_Sorter.ImageType"/>. (<see cref="ImageType.All"/> and <see cref="ImageType.UNK"/> are not supported)</param>
        /// <returns>The file extension for the given <paramref name="imageType"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="imageType"/> is invalid.</exception>
        protected static string GetExtensionForImageType(ImageType imageType) => imageType switch
        {
            ImageType.JPG => ".jpg",
            ImageType.JPEG => ".jpeg",
            ImageType.PNG => ".png",
            ImageType.TIFF => ".tiff",
            ImageType.BMP => ".bmp",
            _ => throw new ArgumentException("Invalid image type", nameof(imageType))
        };
    }
}
