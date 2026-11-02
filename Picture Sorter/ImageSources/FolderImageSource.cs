using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Picture_Sorter.Dialogs;

namespace Picture_Sorter.ImageSources
{
    public sealed class FolderImageSource : IImageSource
    {
        /// <summary>
        /// Gets the physical file path to the source folder.
        /// </summary>
        public string SourcePath { get; }

        /// <summary>
        /// Indicates wether to load the folder recursively.
        /// </summary>
        public bool Recursive { get; }

        /// <inheritdoc/>
        public ImageSourceType SourceType { get; } = ImageSourceType.Folder;

        /// <inheritdoc/>
        public ImageType ImageType { get; }


        /// <summary>
        /// Creates a new <see cref="FolderImageSource"/>.
        /// </summary>
        /// <param name="path">The physical file path to the source folder.</param>
        /// <param name="recursive">Indicates whether to load the folder recursively.</param>
        /// <param name="imageType">The <see cref="ImageType"/> of the images to load.</param>
        public FolderImageSource(string path, bool recursive, ImageType imageType)
        {
            SourcePath = path;
            Recursive = recursive;
            ImageType = imageType;
        }


        /// <inheritdoc/>
        public IEnumerable<FileInfo> Load()
        {
            try
            {
                // Loads all image files from a the source folder
                var files = new DirectoryInfo(SourcePath).GetFiles(
                    "*.*",
                    Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);


                if (ImageType == ImageType.All)
                {
                    // Filters all valid images types
                    return files
                        .Where(x => MainWindow.ValidFileTypes.Contains(Path.GetExtension(x.FullName), StringComparer.OrdinalIgnoreCase));
                }
                else
                {
                    var imageTypeExtension = IImageSource.GetExtensionForImageType(ImageType);

                    // Filter files for selected file type only
                    return files
                        .Where(x => string.Equals(Path.GetExtension(x.FullName), imageTypeExtension, StringComparison.OrdinalIgnoreCase));
                }
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    InformationBoxDialog.Show("Error", $"Unable to load files from '{SourcePath}'.", MessageBoxButton.OK);
                });
            }

            return [];
        }

        /// <inheritdoc/>
        public IEnumerable<FileInfo> LoadRaws()
        {
            try
            {
                // Loads all image files from a the source folder
                var files = new DirectoryInfo(SourcePath).GetFiles(
                    "*.*",
                    Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);


                // Filters all valid raw images types
                return files
                    .Where(x => MainWindow.ValidRawFileTypes.Contains(Path.GetExtension(x.FullName), StringComparer.OrdinalIgnoreCase));
            }
            catch
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    InformationBoxDialog.Show("Error", $"Unable to load raws from '{SourcePath}'.", MessageBoxButton.OK);
                });
            }

            return [];
        }
    }
}
