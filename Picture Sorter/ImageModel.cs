using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Picture_Sorter.ImageTransfers;

namespace Picture_Sorter
{
    /// <summary>
    /// Defines an image.
    /// </summary>
    public sealed class ImageModel : INotifyPropertyChanged
    {
        // Indicates wether the image information (Date, Size, ...) have been loaded already
        private bool _loadedInformation = false;

        // Indicates wether the thumbnail is beeing loaded
        private bool _isThumbnailLoading;

        // The transfer actions associated with this <see cref="ImageModel"/>.
        private readonly List<IImageTransfer> _transfers = [];


        /// <summary>
        /// The path to the physical image file.
        /// </summary>
        public string SourcePath { get; }

        /// <summary>
        /// Indicates wether the physical image file exists.
        /// </summary>
        public bool Exists
        {
            get;
            set
            {
                field = value;

                // Update properties that depend on Exists
                OnPropertyChanged(nameof(Exists));
                OnPropertyChanged(nameof(IsEnabled));
            }
        } = true;

        /// <summary>
        /// The path of the physical associated raw file.
        /// </summary>
        public string RawSourcePath
        {
            get;
            set
            {
                field = value;

                // Update properties that depend on RawSourcePath
                OnPropertyChanged(nameof(RawSourcePath));
                OnPropertyChanged(nameof(HasRaw));
            }
        }

        /// <summary>
        /// Indicates, wether this <see cref="ImageModel"/> has an associated raw file.
        /// </summary>
        public bool HasRaw => RawSourcePath is not null;

        /// <summary>
        /// Gets the <see cref="BitmapImage"/> used as a thumbnail for this <see cref="ImageModel"/>.
        /// </summary>
        /// <remarks>May be null if thumbnail is not loaded yet or loading failed.</remarks>
        public BitmapImage Thumbnail
        {
            get
            {
                // Check if thumbnail is already loaded or is being loaded,
                // if not, load it in a background thread
                if (field is null && !_isThumbnailLoading)
                {
                    // Mark thumbnail as beeing loaded
                    _isThumbnailLoading = true;

                    // Runs the background loading task
                    Task.Run(() =>
                    {
                        try
                        {
                            // Load thumbnail as a bitmap image.
                            BitmapImage bmp = LoadThumbnail();

                            // Update the property on UI thread
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                // Set the loaded thumbnail
                                field = bmp;

                                // Update property changed event for Thumbnail
                                OnPropertyChanged(nameof(Thumbnail));
                            });
                        }
                        catch { /* Ignore thumbnail loading errors */ }
                        finally
                        {
                            // Mark thumbnail loading as finished
                            // regardless of success or failure
                            _isThumbnailLoading = false;
                        }
                    });
                }

                // Return the thumbnail (may be null if not loaded yet)
                return field;
            }
        }

        /// <summary>
        /// Gets the number of this <see cref="ImageModel"/> to display in the UI. (e.g. 1, 2, 3, ...)
        /// </summary>
        public int UINumber { get; }

        /// <summary>
        /// The <see cref="ImageType"/> to the physical image file. (e.g. JPG, PNG, ...)
        /// </summary>
        public ImageType Type
        {
            get;
            private set
            {
                field = value;

                // Update properties that depend on Type
                OnPropertyChanged(nameof(Type));
            }
        } = ImageType.UNK;

        /// <summary>
        /// The capture date of the image
        /// </summary>
        /// <remarks>Relies on the image's <see cref="FileSystemInfo.LastWriteTime"/>.</remarks>
        public DateTime CaptureDate
        {
            get;
            private set
            {
                field = value;

                // Update properties that depend on CaptureDate
                OnPropertyChanged(nameof(CaptureDate));
            }
        } = DateTime.MinValue;

        /// <summary>
        /// The size of the physical image file in bytes.
        /// </summary>
        public long Size
        {
            get;
            private set
            {
                field = value;

                // Update properties that depend on Size
                OnPropertyChanged(nameof(Size));
            }
        } = 0;

        /// <summary>
        /// The width of the image in pixels.
        /// </summary>
        /// <remarks>0 by default when the information has not been loaded yet. Call <see cref="LoadInfos"/>.</remarks>
        public int ResolutionWidth
        {
            get;
            private set
            {
                field = value;

                // Update properties that depend on ResolutionWidth
                OnPropertyChanged(nameof(ResolutionWidth));
            }
        } = 0;

        /// <summary>
        /// The height of the image in pixels.
        /// </summary>
        /// <remarks>0 by default when the information has not been loaded yet. Call <see cref="LoadInfos"/>.</remarks>
        public int ResolutionHeight
        {
            get;
            private set
            {
                field = value;

                // Update properties that depend on ResolutionHeight
                OnPropertyChanged(nameof(ResolutionHeight));
            }
        } = 0;

        /// <summary>
        /// The file name of the image. (e.g. "ImageName.jpg")
        /// </summary>
        public string UIFileName => Path.GetFileName(SourcePath);

        /// <summary>
        /// The name of the folder containing the image. (e.g. "FolderName")
        /// </summary>
        public string UIFolderName => new DirectoryInfo(SourcePath).Parent.Name;

        /// <summary>
        /// The folder containing the image and image name. (e.g. "FolderName/ImageName.jpg")
        /// </summary>
        public string UIShortPath => Path.Combine(UIFolderName, UIFileName);

        
        /// <summary>
        /// Number of copy transfers.
        /// </summary>
        public int CopyCount
        {
            get;
            set
            {
                // Checks if the new value is different from the current value
                // and if the new value is valid (non-negative)
                if (field != value && value >= 0)
                {
                    // Store the old value for comparison
                    var old = field;

                    field = value;

                    // Update properties that depend on CopyCount
                    OnPropertyChanged(nameof(CopyCount));

                    // Evaluate state changes from or to 1
                    bool wasNotZero = old > 0;
                    bool isNotZero = value > 0;

                    // Checks wether state changed from or to 1
                    if (wasNotZero != isNotZero)
                    {
                        // Update properties that depend on a stage change from or to 1
                        OnPropertyChanged(nameof(IsCopied));
                        OnPropertyChanged(nameof(IsTransfered));
                    }
                }
            }
        }

        /// <summary>
        /// Number of move transfers.
        /// </summary>
        public int MoveCount
        {
            get;
            set
            {
                // Checks if the new value is different from the current value
                // and if the new value is valid (non-negative)
                if (field != value && value >= 0)
                {
                    // Store the old value for comparison
                    var old = field;

                    field = value;

                    // Update properties that depend on MoveCount
                    OnPropertyChanged(nameof(MoveCount));

                    // Evaluate state changes from or to 1
                    bool wasNotZero = old > 0;
                    bool isNotZero = value > 0;

                    // Checks wether state changed from or to 1
                    if (wasNotZero != isNotZero)
                    {
                        // Update properties that depend on a stage change from or to 1
                        OnPropertyChanged(nameof(IsMoved));
                        OnPropertyChanged(nameof(IsTransfered));
                        OnPropertyChanged(nameof(IsEnabled));
                    }
                }
            }
        }

        /// <summary>
        /// Number of delete transfers.
        /// </summary>
        public int DeleteCount
        {
            get;
            set
            {
                // Checks if the new value is different from the current value
                // and if the new value is valid (non-negative)
                if (field != value && value >= 0)
                {
                    // Store the old value for comparison
                    var old = field;

                    field = value;

                    // Update properties that depend on DeleteCount
                    OnPropertyChanged(nameof(DeleteCount));

                    // Evaluate state changes from or to 1
                    bool wasNotZero = old > 0;
                    bool isNotZero = value > 0;

                    // Checks wether state changed from or to 1
                    if (wasNotZero != isNotZero)
                    {
                        // Update properties that depend on a stage change from or to 1
                        OnPropertyChanged(nameof(IsDeleted));
                        OnPropertyChanged(nameof(IsTransfered));
                        OnPropertyChanged(nameof(IsEnabled));
                    }
                }
            }
        }

        /// <summary>
        /// Indicates wether the image was copied.
        /// </summary>
        public bool IsCopied => CopyCount > 0;

        /// <summary>
        /// Indicates wether the image was moved.
        /// </summary>
        public bool IsMoved => MoveCount > 0;

        /// <summary>
        /// Indicates wether the image was deleted.
        /// </summary>
        public bool IsDeleted => DeleteCount > 0;

        /// <summary>
        /// Indicates, wether the image has been transfered.
        /// </summary>
        public bool IsTransfered => IsCopied || IsMoved || IsDeleted;

        /// <summary>
        /// Indicates, wether the image is available.
        /// </summary>
        public bool IsEnabled => !(IsMoved || IsDeleted || !Exists);


        /// <summary>
        /// Indicates wether the image is marked in the UI.
        /// </summary>
        public bool IsMarked
        {
            get;
            set
            {
                field = value;

                // Update properties that depend on IsMarked
                OnPropertyChanged(nameof(IsMarked));
            }
        } = false;

        /// <summary>
        /// Indicates wether the image is in opned in the UI's split view mode.
        /// </summary>
        public bool IsInSplitView
        {
            get;
            set
            {
                field = value;

                // Update properties that depend on IsInSplitView
                OnPropertyChanged(nameof(IsInSplitView));
            }
        } = false;


        /// <summary>
        /// Creates a new <see cref="ImageModel"/> based on <paramref name="file"/> and <paramref name="uiNumber"/>.
        /// </summary>
        /// <param name="file">The <see cref="FileInfo"/> representing the image file.</param>
        /// <param name="uiNumber">The number associated with the image for UI purposes.</param>
        public ImageModel(FileInfo file, int uiNumber)
        {
            SourcePath = file.FullName;
            CaptureDate = file.LastWriteTime;
            Size = file.Length;
            UINumber = uiNumber;
        }


        // PropertyChanged event and method to raise the event and track changes in properties
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        /// <summary>
        /// Load information about the image asynchronously.
        /// </summary>
        public async void LoadInfos()
        {
            // If already loaded the informations, return
            if (_loadedInformation) { return; }

            // Check if the image file exists
            if (!File.Exists(SourcePath))
            {
                Exists = false;
                return;
            }

            // Run the loading task in a background thread to avoid blocking the UI
            await Task.Run(() =>
            {
                try
                {
                    // Get original dimensions without locking the file
                    using (var fileStream = new FileStream(SourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        BitmapDecoder decoder = BitmapDecoder.Create(
                            fileStream,
                            BitmapCreateOptions.DelayCreation,
                            BitmapCacheOption.None);

                        // Sets the resolution width and height based on the first frame of the image
                        ResolutionWidth = decoder.Frames[0].PixelWidth;
                        ResolutionHeight = decoder.Frames[0].PixelHeight;
                    }

                    // Sets the image file type based on the file extension
                    Type = Enum.Parse<ImageType>(Path.GetExtension(SourcePath)[1..], true);

                    // Mark information as loaded
                    _loadedInformation = true;
                }
                catch { /* Ignore exceptions during information loading */ }
            });
        }

        /// <summary>
        /// Add <paramref name="transfer"/> to this <see cref="ImageModel"/>.
        /// </summary>
        /// <param name="transfer">The <see cref="IImageTransfer"/> to add.</param>
        public void AddTransfer(IImageTransfer transfer)
        {
            _transfers.Add(transfer);
        }

        /// <summary>
        /// Remove <paramref name="transfer"/> from this <see cref="ImageModel"/>.
        /// </summary>
        /// <param name="transfer">The <see cref="IImageTransfer"/> to remove.</param>
        public void RemoveTransfer(IImageTransfer transfer)
        {
            _transfers.Remove(transfer);
        }

        /// <summary>
        /// Gets the last <see cref="IImageTransfer"/> added to this <see cref="ImageModel"/>.
        /// </summary>
        /// <returns>The last <see cref="IImageTransfer"/>.</returns>
        public IImageTransfer GetLastTransfer()
        {
            return _transfers[^1];
        }

        /// <summary>
        /// Loads the thumbnail of this <see cref="ImageModel"/>.
        /// </summary>
        /// <returns>The thumbnail as a <see cref="BitmapImage"/>.</returns>
        private BitmapImage LoadThumbnail()
        {
            // Create cached bitmap for performance
            BitmapImage bmp = new();
            bmp.BeginInit();
            bmp.UriSource = new Uri(SourcePath, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.DecodePixelWidth = 70; // Decode thumbnails with max. 70 pixels width
            bmp.EndInit();

            // Freeze bitmap for preformance.
            bmp.Freeze();

            return bmp;
        }
    }
}
