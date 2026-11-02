using System.Collections.Generic;
using System.IO;

namespace Picture_Sorter.ImageSources
{
    public sealed class FileImageSource : IImageSource
    {
        /// <summary>
        /// Gets the physical file path to the source file.
        /// </summary>
        public string SourcePath { get; }

        /// <inheritdoc/>
        public ImageSourceType SourceType { get; } = ImageSourceType.File;

        /// <inheritdoc/>
        public ImageType ImageType { get; }


        /// <summary>
        /// Creates a new <see cref="FileImageSource"/>.
        /// </summary>
        /// <param name="path">The physical file path to the source file.</param>
        /// <param name="imageType">The <see cref="ImageType"/> of the images to load.</param>
        public FileImageSource(string path, ImageType imageType)
        {
            SourcePath = path;
            ImageType = imageType;
        }


        /// <inheritdoc/>
        public IEnumerable<FileInfo> Load()
        {
            // Filter selected file type only
            if (ImageType == ImageType.All || string.Equals(Path.GetExtension(SourcePath), IImageSource.GetExtensionForImageType(ImageType), System.StringComparison.OrdinalIgnoreCase))
            {
                return [new FileInfo(SourcePath)];
            }
            else
            {
                return [];
            }
        }

        /// <inheritdoc/>
        public IEnumerable<FileInfo> LoadRaws()
        {
            // Try to find a matching RAW file for the selected image file
            if (TryFindRawForImage(out string rawPath))
            {
                return [new FileInfo(rawPath)];
            }

            return [];
        }

        /// <summary>
        /// Trys to find a raw file matching the image name in the same folder.
        /// </summary>
        /// <param name="rawPath">If <see langword="true"/>, contains the path to the found raw file; otherwise, <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if a matching raw file is found; otherwise, <see langword="false"/>.</returns>
        private bool TryFindRawForImage(out string rawPath)
        {
            // Extract the directory and file name from the source path
            string directory = Path.GetDirectoryName(SourcePath);
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(SourcePath);

            // Search for a matching raw file with the same base name
            // Trys all known raw file extensions
            foreach (var ext in MainWindow.ValidRawFileTypes)
            {
                // Creates the potential raw filePath
                string potentialRawPath = Path.Combine(directory, fileNameWithoutExt + ext);

                // If the potential raw file exists, return
                if (File.Exists(potentialRawPath))
                {
                    rawPath = potentialRawPath;
                    return true;
                }
            }

            // Nor raw file was found
            rawPath = null;
            return false;
        }
    }
}
