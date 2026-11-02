using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using Picture_Sorter.Dialogs;
using Picture_Sorter.ImageDestinations;
using Picture_Sorter.ImageDestinations.Templates;
using Picture_Sorter.ImageSources;
using Picture_Sorter.ImageTransfers;
using Picture_Sorter.Json;
using Picture_Sorter.Themes;

#pragma warning disable CA1416 // Validate platform compatibility
namespace Picture_Sorter
{
    /// <summary>
    /// The main window of the application.
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // List of supported file types
        public static readonly string[] ValidFileTypes = [".jpg", ".jpeg", ".png", ".bmp", ".tiff"];

        // List of image raw file types
        public static readonly string[] ValidRawFileTypes = [
            ".arw", // Sony
            ".cr2", // Canon
            ".cr3", // Canon
            ".nef", // Nikon
            ".nrw", // Nikon
            ".raf", // Fujifilm
            ".rw2", // Panasonic (Lumix)
            ".rwl", // Leica
            ".3fr", // Hasselblad
            ".fff", // Hasselblad
            ".dng", // Universal
        ];

        // Path of template file
        private static readonly string _templateFilePath = Path.Combine(AppContext.BaseDirectory, "templates.json");

        // Image enabled condition
        private static readonly Func<ImageModel, bool> IsEnabledCondition = (x) => x.IsEnabled;

        // Image marked condition
        private static readonly Func<ImageModel, bool> IsMarkedCondition = (x) => x.IsMarked;


        // The current loaded root folder
        public List<IImageSource> LoadedSources { get; } = [];

        // All images in the selected folder
        public ObservableCollection<ImageModel> Images
        {
            get;
            private set
            {
                field = value;

                OnPropertyChanged(nameof(Images));
            }
        } = [];

        // Number of images transfered
        public int TransferedImageCount => Images.Count(x => x.IsTransfered);

        // All file transfers
        public ObservableCollection<IImageTransfer> Transfers { get; } = [];

        // Destination templates
        public ObservableCollection<DestinationTemplate> DestinationTemplates { get; } = [new("No template") { IsDefault = true }];

        // The current selected item
        public DestinationTemplate CurrentDestinationTemplate 
        { 
            get;
            set
            {
                field = value;

                OnPropertyChanged(nameof(CurrentDestinationTemplate));
            }
        }

        // All destinations
        private ObservableCollection<IImageDestination> CurrentDestinations => CurrentDestinationTemplate.Destinations;

        // Indicates wether to load the full image resolution
        public bool LoadFullResolution
        { 
            get; 
            set
            {
                field = value;
                OnPropertyChanged(nameof(LoadFullResolution));

                ReloadImageInPlace(_currentViewAngle);
            }
        } = false;

        // Indicates wether to move by default
        public bool MoveByDefault
        {
            get;
            set
            {
                field = value;
                OnPropertyChanged(nameof(MoveByDefault));
            }
        } = false;

        // Indicates wether the search box is visible
        public bool IsSearchVisible
        {
            get;
            set
            {
                field = value;
                OnPropertyChanged(nameof(IsSearchVisible));
            }
        } = false;

        // Indicates wether a loading is in progress
        public bool IsLoading
        {
            get;
            private set
            {
                field = value;

                OnPropertyChanged(nameof(IsLoading));

                // Disable all controls while loading
                RootGrid.IsEnabled = !value;
            }
        } = false;


        // Search counter
        public SearchCounter SearchCounter { get; } = new(0, 0);

        // Resource dictionary for light mode resources
        private readonly ResourceDictionary _lightTheme = new() { Source = new("Themes/LightTheme.xaml", UriKind.Relative) };

        // Resource dictionary for dark mode resources
        private readonly ResourceDictionary _darkTheme = new() { Source = new("Themes/DarkTheme.xaml", UriKind.Relative) };


        // Current theme
        public Theme Theme
        {
            get;
            set
            {
                // Replace the merged dictionary cleanly
                var mergedDictionaries = Application.Current.Resources.MergedDictionaries;

                if (value == Theme.Light)
                {
                    mergedDictionaries.Remove(_darkTheme);
                    mergedDictionaries.Add(_lightTheme);


                }
                else if (value == Theme.Dark)
                {
                    mergedDictionaries.Remove(_lightTheme);
                    mergedDictionaries.Add(_darkTheme);
                }

                field = value;
            }
        } = Theme.Dark;


        // The current image
        public ImageModel CurrentImage => Images[_currentImageIndex];

        // The current image in the split view
        public ImageModel CurrentSplitViewImage
        {
            get;
            private set
            {
                field = value;

                OnPropertyChanged(nameof(CurrentSplitViewImage));
            }
        } = null;

        // The current number of loaded images
        public int ImageCount => Images.Count;

        // The current number of destinations
        public int DestinationCount => CurrentDestinations.Count;

        // The current number of transfers
        public int TransferCount => Transfers.Count;


        // Default/Fallback image
        private readonly BitmapImage _default;

        // Debounce when dragging the slider
        private readonly DispatcherTimer _imageLoadDebounceTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(20),
        };

        // Debounce when typing in search
        private readonly DispatcherTimer _searchDebounceTimer = new()
        {
            Interval = TimeSpan.FromMilliseconds(250),
        };

        // Event to track changes
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        public MainWindow()
        {
            // Check if operating system is windows
            if (!OperatingSystem.IsWindows())
            {
                MessageBox.Show("Your operating system is currently not supported. Only Windows!", "Operating system not supported!", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }

            // Sets the data context
            this.DataContext = this;

            // Initialize default components
            InitializeComponent();

            // Sets the default image
            _default = FindResource("DefaultImage") as BitmapImage;


            // Whenever the collection or an item in the collection changes
            Images.CollectionChanged += (sender, e) =>
            {
                // Handle items added, subscribe
                if (e.NewItems is not null)
                {
                    foreach (ImageModel item in e.NewItems)
                    {
                        item.PropertyChanged += Image_PropertyChanged;
                    }
                }

                // Handle items removed, unsubscribe
                if (e.OldItems is not null)
                {
                    foreach (ImageModel item in e.OldItems)
                    {
                        item.PropertyChanged -= Image_PropertyChanged;
                    }
                }
            };



#if DEBUG
            // Load images from current working directory
            LoadSources([new FolderImageSource(Environment.CurrentDirectory, false, ImageType.All)]);
#endif


            
            foreach(var arg in Environment.GetCommandLineArgs())
            {
                // Set theme
                if (arg.Equals("--light-mode", StringComparison.OrdinalIgnoreCase))
                {
                    Theme = Theme.Light;
                }
                else if (arg.Equals("--dark-mode", StringComparison.OrdinalIgnoreCase))
                {
                    Theme = Theme.Dark;
                }
                else if (arg.Equals("--mode-move", StringComparison.OrdinalIgnoreCase))
                {
                    MoveByDefault = true;
                }
                else if (arg.Equals("--mode-copy", StringComparison.OrdinalIgnoreCase))
                {
                    MoveByDefault = false;
                }
                else if (arg.Equals("--full-resolution", StringComparison.OrdinalIgnoreCase))
                {
                    LoadFullResolution = true;
                }
            }

            // Load templates
            LoadDestinationTemplates(_templateFilePath);

            RootGrid.Focus();
            // Sets focus to the image
            Image.Focus();


            // Imagve loading on drag
            _imageLoadDebounceTimer.Tick += async (s, e) =>
            {
                _imageLoadDebounceTimer.Stop();

                var index = (int)ImageSlider.Value - 1;

                // If image is diabled, only scroll to position
                if (ImageListBox.SelectedItem is ImageModel image && !image.IsEnabled)
                {
                    ImageListBox.ScrollIntoView(image);
                    ThumbnailListBox.ScrollIntoView(image);
                }

                // Loads the image
                LoadImage(index);
            };

            // Searching on type
            _searchDebounceTimer.Tick += async (s, e) =>
            {
                _searchDebounceTimer.Stop();

                if (string.IsNullOrEmpty(SearchBox.Text))
                {
                    SearchCounter.TotalResults = 0;
                    SearchCounter.CurrentResultNumber = 0;
                    OnPropertyChanged(nameof(SearchCounter));
                }
                else
                {
                    SearchCounter.TotalResults = Images.Count(x => SearchFunc(x, SearchBox.Text));
                    SearchCounter.CurrentResultNumber = Math.Min(SearchCounter.TotalResults, 1);
                    OnPropertyChanged(nameof(SearchCounter));

                    FirstImage(x => SearchFunc(x, SearchBox.Text));
                }
            };
        }

        private void Image_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // If any item's IsTransfered property changes, update count
            if (e.PropertyName == nameof(ImageModel.IsTransfered))
            {
                OnPropertyChanged(nameof(TransferedImageCount));
            }
        }

        #region Loading
        private async void LoadSources<T>(IEnumerable<T> sources, bool add = false, bool findRaws = false, ImageOrderType orderType = ImageOrderType.LastWriteAsc) where T : IImageSource
        {
            // If no images are already loaded, load new
            if (ImageCount == 0) { add = false; }

            // Close split view, since we dont know if loading images succeeds
            if (!add) { ToggleSplitViewBtn.IsChecked = false; }

            // Show loading screen
            IsLoading = true;

            // Sets the loading text
            LoadingTextBlock.Text = add ? "Loading additional sources..." : "Loading sources...";

            // New list of loaded images
            List<ImageModel> newImages = null;

            try
            {
                // Load non blocking
                await Task.Run(() =>
                {
                    // All loaded image paths
                    List<FileInfo> files = [];

                    // All loaded raw image paths
                    List<FileInfo> rawFiles = findRaws ? [] : null;

                    // Load all folders
                    foreach (var source in sources)
                    {
                        files.AddRange(source.Load());

                        if (findRaws)
                        {
                            rawFiles.AddRange(source.LoadRaws());
                        }
                    }

                    // Show message that no images were found
                    if (files.Count == 0)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            InformationBoxDialog.Show("No images found", $"No images found in '{string.Join(", ", sources.Select(x => x.SourcePath))}'. Valid file types are '{string.Join(", ", ValidFileTypes)}'", MessageBoxButton.OK);
                        });
                    }

                    // Remove duplicates
                    var filteredFiles = files.DistinctBy(x => x.FullName, StringComparer.OrdinalIgnoreCase);

                    // If adding images, remove all already existing images
                    if (add)
                    {
                        // Get exisitng image paths
                        HashSet<string> existingPaths = new(Images.Select(x => x.SourcePath), StringComparer.OrdinalIgnoreCase);

                        // Remove existing images from new images
                        filteredFiles = filteredFiles.Where(x => !existingPaths.Contains(x.FullName)).ToList();
                    }


                    // Order file paths
                    FileInfo[] orderedFiles = OrderByImageOrderType(filteredFiles, orderType).ToArray();


                    // Creates a new list (adds existing images if add is enabled)
                    newImages = add ? new(ImageCount + orderedFiles.Length) : new(orderedFiles.Length);

                    // Add existing images
                    if (add) { newImages.AddRange(Images); }

                    // Number of images to load
                    var loadCount = orderedFiles.Length;

                    // Adds the images
                    var offset = add ? ImageCount : 0;
                    int i = 0;
                    foreach (var file in orderedFiles)
                    {
                        // Updates the loading text every 10 images
                        if (i % 10 == 0)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                LoadingTextBlock.Text = $"Loaded {i + 1}/{loadCount} image(s)...";
                            });
                        }


                        // Searches for an assocciated raw file
                        string rawFilePath = null;
                        if (findRaws)
                        {
                            var rawFile = rawFiles.FirstOrDefault(x => x.FullName.Equals(Path.ChangeExtension(file.FullName, Path.GetExtension(x.Name)), StringComparison.OrdinalIgnoreCase));

                            if (rawFile is not null)
                            {
                                rawFilePath = rawFile.FullName;
                            }
                        }


                        // Creates the image
                        var image = new ImageModel(file, i + 1 + offset)
                        {
                            RawSourcePath = rawFilePath
                        };

                        // Add the image
                        newImages.Add(image);

                        i++;
                    }


                    // Updates the loading text
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        LoadingTextBlock.Text = $"Finishing loading...";
                    });


                    // Skips when reloading
                    if (!ReferenceEquals(sources, LoadedSources))
                    {
                        if (!add)
                        {
                            // Clear loaded sources
                            LoadedSources.Clear();
                        }

                        // Add loaded sources
                        foreach (var source in sources)
                        {
                            // If source is already saved, skip
                            if (LoadedSources.Contains(source)) { continue; }

                            // Add source
                            LoadedSources.Add(source);
                        }
                    }
                });
            }
            catch { }
            finally
            {
                // Replace current image collection
                Images = new(newImages);

                // Sets the maximum scroll length
                ImageSlider.Maximum = ImageCount;

                // If loading new, select the first image
                if (!add)
                {
                    // Resets the index
                    _currentImageIndex = 0;

                    // Loads the first image
                    LoadImage(0, true);
                }

                // Hide loading screen
                IsLoading = false;

                // Sets the focus to the image
                Image.Focus();
            }
        }

        private static IOrderedEnumerable<FileInfo> OrderByImageOrderType(IEnumerable<FileInfo> files, ImageOrderType orderType)
        {
            if (orderType == ImageOrderType.LastWriteAsc)
            {
                return files.OrderBy(x => x.LastWriteTime);
            }
            else if (orderType == ImageOrderType.LastWriteDesc)
            {
                return files.OrderByDescending(x => x.LastWriteTime);
            }
            else if (orderType == ImageOrderType.CreationTimeAsc)
            {
                return files.OrderBy(x => x.CreationTime);
            }
            else if (orderType == ImageOrderType.CreationTimeDesc)
            {
                return files.OrderByDescending(x => x.CreationTime);
            }
            else if (orderType == ImageOrderType.SizeAsc)
            {
                return files.OrderBy(x => x.Length);
            }
            else if (orderType == ImageOrderType.SizeDesc)
            {
                return files.OrderByDescending(x => x.Length);
            }
            else if (orderType == ImageOrderType.NameAsc)
            {
                return files.OrderBy(x => x.Name);
            }
            else if (orderType == ImageOrderType.NameDesc)
            {
                return files.OrderByDescending(x => x.Name);
            }
            else if (orderType == ImageOrderType.DirectoryAsc)
            {
                return files.OrderBy(x => x.Directory.Name);
            }
            else if (orderType == ImageOrderType.DirectoryDesc)
            {
                return files.OrderByDescending(x => x.Directory.Name);
            }
            else
            {
                return files.Order();
            }
        }


        // The current loaded image
        private BitmapImage _currentImage;

        // The current viewing angle of the current image
        private int _currentViewAngle = 0;

        // The current selected image in folder
        private int _currentImageIndex = 0;

        private void LoadImage(int index, bool loadAnyway = false)
        {
            // If not initialized, return
            if (!IsInitialized && !loadAnyway) { return; }

            // If invalid index, return
            if ((index == _currentImageIndex && !loadAnyway) || index == -1) { return; }

            // Loads the default image, if no images are available
            if (ImageCount == 0)
            {
                Image.Source = _default;
                return;
            }

            var image = Images[index];

            

            // Load new bitmap
            BitmapImage bitmap;
            

            // If file is not found, load default image
            if (!File.Exists(image.SourcePath))
            {
                // Disable not exiting item
                image.Exists = false;

                bitmap = _default;
            }
            else
            {
                try
                {
                    bitmap = LoadImage(image);
                }
                catch
                {
                    InformationBoxDialog.Show("Error", $"Unable to load image '{image.SourcePath}'.", MessageBoxButton.OK);
                    bitmap = _default;
                    image.Exists = false;
                }
            }

            
            // Resets the view angle to 0
            _currentViewAngle = 0;

            // Sets the current image
            _currentImage = bitmap;

            // Sets the current image index
            _currentImageIndex = index;


            // Resets the image zoom and position
            // NOTE: Rotation is automatically reseted by loading the new image
            ResetImageZoomLevel();
            ResetImagePosition();


            // Sets the image source
            Image.Source = _currentImage;

            // Loads the info labels
            image.LoadInfos();


            if (ImageListBox.SelectedIndex != index)
            {
                // Sets the thumbnail previews
                ImageListBox.SelectedIndex = index;
            }


            // Scroll the thumbnail into view automatically if using arrow keys
            ImageListBox.ScrollIntoView(image);
            ThumbnailListBox.ScrollIntoView(image);

            LookAhead(index);

            // Sets the image slider
            if ((int)ImageSlider.Value != index + 1)
            {
                ImageSlider.Value = index + 1;
            }
        }

        private void ReloadImageInPlace(int angle)
        {
            // Store old view angle
            var oldAngle = _currentViewAngle;

            // Store current view settings
            var zoomLevel = Image.ZoomRatio;
            var posX = Image.TranslateX;
            var posY = Image.TranslateY;
            

            // Reload current image
            LoadImage(_currentImageIndex, loadAnyway: true);

            // Appl Rotation
            ApplyViewAngle(angle);

            // Calculate the delta angle
            int deltaAngle = ((angle - oldAngle) % 360 + 360) % 360;

            // Adjusted positions
            double adjustedX;
            double adjustedY;

            // Calculate the aspect ratio of the image
            double aspect = CurrentImage.ResolutionWidth / (double)CurrentImage.ResolutionHeight;

            // Translate the image position based on the delta angle
            switch (deltaAngle)
            {
                case 90:
                    adjustedX = -posY * aspect;
                    adjustedY = posX / aspect;
                    break;

                case 180:
                    adjustedX = -posX;
                    adjustedY = -posY;
                    break;

                case 270:
                    adjustedX = posY * aspect;
                    adjustedY = -posX / aspect;
                    break;

                default: // 0 Grad
                    adjustedX = posX;
                    adjustedY = posY;
                    break;
            }
            
            // Disable animation while repositioning
            Image.DisableAnimation = true;

            // Recover image view setting
            Image.ZoomRatio = zoomLevel;
            Image.TranslateX = adjustedX;
            Image.TranslateY = adjustedY;

            // Reenable animation after repositioning
            Image.DisableAnimation = false;
        }

        private BitmapImage LoadImage(ImageModel image)
        {
            BitmapImage bitmap = new();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(image.SourcePath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad; // Prevents file locking
            if (!LoadFullResolution) { bitmap.DecodePixelWidth = 1920; }
            bitmap.EndInit();
            bitmap.Freeze();
            return bitmap;
        }

        // Remebrs the last opened directory for source locations
        private string LastOpenedDirectory = string.Empty;

        internal void OpenFolder(bool recursive = false, bool add = false, bool findRaws = false)
        {
            // Shows an open folder dialog
            OpenFolderDialog openFolderDialog = new()
            {
                Title = recursive ? "Open folder(s) (recursively)" : "Open folder(s)",
                DereferenceLinks = true,
                Multiselect = true, // Allow to select multiple folders
                InitialDirectory = LastOpenedDirectory, // Opens the last opend directory
            };

            if (openFolderDialog.ShowDialog() == true)
            {
                var imageType = ImageType.All;
                var orderType = ImageOrderType.LastWriteAsc;

                // Prompt for loading options
                var result = LoadingSourceOptionsDialog.Show(ref recursive, ref add, ref findRaws, ref imageType, ref orderType);

                if (result == MessageBoxResult.OK)
                {
                    // Loads the selected folder
                    LoadSources(openFolderDialog.FolderNames.Select(x => new FolderImageSource(x, recursive, imageType)), add: add, findRaws: findRaws, orderType: orderType);

                    // Sets the last opened directory
                    LastOpenedDirectory = openFolderDialog.FolderNames[^1];
                }
            }
        }

        internal async void ReloadFolders(bool refresh = false, bool findRaws = false)
        {
            // Saves the current index
            var currentIndex = _currentImageIndex;

            // Reload all folders
            LoadSources(LoadedSources, add: refresh, findRaws: findRaws);

            if (refresh)
            {
                // Show loading screen
                IsLoading = true;

                // Sets the loading text
                LoadingTextBlock.Text = "Refreshing image status and data...";

                try
                {
                    await Task.Run(() =>
                    {
                        var loadCount = Images.Count;

                        // All loaded raw image paths
                        List<FileInfo> rawFiles = findRaws ? LoadedSources.SelectMany(x => x.LoadRaws()).ToList() : null;

                        // Refresh the status of all images
                        int i = 0;
                        foreach (var image in Images)
                        {
                            // Updates the loading text every 10 images
                            if (i % 10 == 0)
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    LoadingTextBlock.Text = $"Refreshed {i + 1}/{loadCount} image(s)...";
                                });
                            }

                            // Reenables manually recovered images
                            if (File.Exists(image.SourcePath))
                            {
                                image.DeleteCount = 0;
                                image.MoveCount = 0;
                                image.Exists = true;


                                // Looks if the raw image still exists
                                if (image.HasRaw && !File.Exists(image.RawSourcePath))
                                {
                                    image.RawSourcePath = null;
                                }

                                // Serach for raws
                                if (image.RawSourcePath is null && findRaws)
                                {
                                    // Searches for an assocciated raw file
                                    string rawFilePath = null;
                                    var rawFile = rawFiles.FirstOrDefault(x => x.FullName.Equals(Path.ChangeExtension(image.SourcePath, Path.GetExtension(x.Name)), StringComparison.OrdinalIgnoreCase));

                                    if (rawFile is not null)
                                    {
                                        rawFilePath = rawFile.FullName;
                                    }

                                    image.RawSourcePath = rawFilePath;
                                }
                            }
                            else
                            {
                                image.Exists = false;
                            }

                            i++;
                        }
                    });
                }
                catch { }
                finally
                {
                    // Hide loading screen
                    IsLoading = false;
                }
            }

            // Trys to load the same image again
            if (currentIndex < ImageCount)
            {
                LoadImage(currentIndex);
            }

            // Sets the focus to the image
            Image.Focus();
        }
        #endregion

        #region Image Rotation
        private void ApplyViewAngle(int newAngle)
        {
            // If no image is available, return
            if (ImageCount == 0) { return; }

            if (newAngle == 0)
            {
                // If back to 0, just use the original cached bitmap directly
                Image.Source = _currentImage;
            }
            else
            {
                try
                {
                    // Creates a new rotated image source
                    TransformedBitmap rotatedBitmap = new();
                    rotatedBitmap.BeginInit();
                    rotatedBitmap.Source = _currentImage;
                    rotatedBitmap.Transform = new RotateTransform(newAngle);
                    rotatedBitmap.EndInit();

                    // Sets the image source
                    Image.Source = rotatedBitmap;
                }
                catch
                {
                    InformationBoxDialog.Show("Error", $"Unable to rotate image '{_currentImage?.UriSource.AbsolutePath}'.", MessageBoxButton.OK);
                }
            }
            
            _currentViewAngle = newAngle;
        }

        private void RotateLeft()
        {
            var newAngle = (_currentViewAngle - 90 + 360) % 360;
            ReloadImageInPlace(newAngle);
        }

        private void RotateRight()
        {
            var newAngle = (_currentViewAngle + 90) % 360;
            ReloadImageInPlace(newAngle);
        }
        #endregion

        #region Image Zoom
        private void ZoomSlider_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Image.ZoomRatio = 1;

            ResetImagePosition();
        }

        private void ZoomOutBtn_Click(object sender, RoutedEventArgs e)
        {
            Image.ZoomOut();
        }

        private void ZoomInBtn_Click(object sender, RoutedEventArgs e)
        {
            Image.ZoomIn();
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsInitialized) { return; }

            // Enables/disables the zoom in Button
            if (ZoomSlider.Value == ZoomSlider.Maximum) { ZoomInBtn.IsEnabled = false; }
            else { ZoomInBtn?.IsEnabled = true; }

            // Enables/disables the zoom out Button
            if (ZoomSlider.Value == ZoomSlider?.Minimum) { ZoomOutBtn.IsEnabled = false; }
            else { ZoomOutBtn.IsEnabled = true; }
        }

        private void TogglePunshIn(bool extraClose = false)
        {
            if (extraClose && Image.ZoomRatio <= 2)
            {
                // Zoom to 400%
                Image.ZoomRatio = 4;
            }
            else if (Image.ZoomRatio > 1)
            {
                ResetImageZoomLevel();
                ResetImagePosition();
            }
            else
            {
                // Zoom to 200%
                Image.ZoomRatio = 2;
            }
        }
        #endregion

        #region Image Controls
        private void RotateLeftBtn_Click(object sender, RoutedEventArgs e) => RotateLeft();

        private void RotateRightBtn_Click(object sender, RoutedEventArgs e) => RotateRight();

        private void SaveImageBtn_Click(object sender, RoutedEventArgs e) => SaveImage(IsControl);

        private void DeleteImageBtn_Click(object sender, RoutedEventArgs e) => DeleteImage(IsControl);

        private void MarkImageBtn_Click(object sender, RoutedEventArgs e) => ToggleImageMark();

        private void ToggleImageMark()
        {
            // If no image is available, return
            if (ImageCount == 0) { return; }

            ToggleImageMark(CurrentImage);
        }

        private void ToggleImageMark(ImageModel image)
        {
            // Toggle marked status
            image.IsMarked = !image.IsMarked;
        }
        #endregion

        #region Image Navigation
        private void NextImage(Func<ImageModel, bool> condition = null)
        {
            var count = ImageCount - 1;

            // Default condition
            condition ??= IsEnabledCondition;

            // Searches for the next image matching the condition
            int index = _currentImageIndex;
            while (index < count)
            {
                index++;
                if (condition(Images[index]))
                {
                    LoadImage(index);

                    break;
                }
            }
        }

        private void PreviousImage(Func<ImageModel, bool> condition = null)
        {
            // Default condition
            condition ??= IsEnabledCondition;

            // Searches for the previous image matching the condition
            int index = _currentImageIndex;
            while (index > 0)
            {
                index--;
                if (condition(Images[index]))
                {
                    LoadImage(index);
                    LookBefore(index);

                    break;
                }
            }
        }

        private void LastImage(Func<ImageModel, bool> condition = null)
        {
            // Default condition
            condition ??= IsEnabledCondition;

            // Searches for the last image matching the condition
            int index = ImageCount;
            while (index > 0)
            {
                index--;
                if (condition(Images[index]))
                {
                    LoadImage(index);
                    LookBefore(index);
                    break;
                }
            }
        }

        private void FirstImage(Func<ImageModel, bool> condition = null)
        {
            var count = ImageCount - 1;

            // Default condition
            condition ??= IsEnabledCondition;

            // Searches for the first image matching the condition
            int index = -1;
            while (index < count)
            {
                index++;
                if (condition(Images[index]))
                {
                    LoadImage(index);

                    break;
                }
            }
        }

        private void LookAhead(int index)
        {
            // Displays a positive look ahead
            var aheadIndex = Math.Min(ImageCount - 1, index + 2);
            if (aheadIndex != index)
            {
                ImageListBox.ScrollIntoView(Images[aheadIndex]);
                ThumbnailListBox.ScrollIntoView(Images[aheadIndex]);
            }
        }

        private void LookBefore(int index)
        {
            // Displays a negative look ahead
            var beforeIndex = Math.Max(0, index - 2);
            if (beforeIndex != index)
            {
                ImageListBox.ScrollIntoView(Images[beforeIndex]);
                ThumbnailListBox.ScrollIntoView(Images[beforeIndex]);
            }
        }

        private void PreviousImageBtn_Click(object sender, RoutedEventArgs e) => PreviousImage(IsShift ? IsMarkedCondition : IsEnabledCondition);

        private void NextImageBtn_Click(object sender, RoutedEventArgs e) => NextImage(IsShift ? IsMarkedCondition : IsEnabledCondition);

        private void ImageListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If not initialized, return
            if (!IsInitialized) { return; }

            if (ImageListBox.SelectedItem is ImageModel image && image.IsEnabled)
            {
                // Loads the selected image
                LoadImage(ImageListBox.SelectedIndex);
            }

            // Enables/disables the previous image button in the overlay
            if (ImageListBox.SelectedIndex <= 0) { PreviousImageOverlayBtn.IsEnabled = false; }
            else { PreviousImageOverlayBtn.IsEnabled = true; }

            // Enables/disables the next image button in the overlay
            if (ImageListBox.SelectedIndex >= ImageCount - 1) { NextImageOverlayBtn.IsEnabled = false; }
            else { NextImageOverlayBtn.IsEnabled = true; }
        }

        private void ImageSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // If not initialized, return
            if (!IsInitialized) { return; }

            if ((int)ImageSlider.Value - 1 != _currentImageIndex)
            {
                // Restart the timer every time the value changes
                _imageLoadDebounceTimer.Stop();
                _imageLoadDebounceTimer.Start();
            }
        }

        private void PreviewThumbnailListBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Selects the next/previous image
            if (e.Delta > 0) { PreviousImage(); }
            else if (e.Delta < 0) { NextImage(); }
        }

        internal void JumpTo()
        {
            // Prompt for image number
            var result = TextEditDialog.Show("Jump to image", "Jump to:", (_currentImageIndex + 1).ToString(), 
                x => int.TryParse(x, out var number) && number >= 1 && number <= ImageCount,
                out string input);

            if (result == MessageBoxResult.OK && int.TryParse(input, out var index))
            {
                // Correct for zero based indexing
                index--;
                var lastValidIndex = ImageCount - 1;

                bool foundEnabled;

                if (index >= 0 && index <= lastValidIndex && Images[index].IsEnabled)
                {
                    foundEnabled = true;
                }
                // If requested image is not enabled, search around it
                else
                {
                    foundEnabled = false;

                    // Searches the next available image around the requested one (-1, +1, -2, +2) up to 5 distance
                    for (int offset = 1; offset <= 5; offset++)
                    {
                        int left = index - offset;
                        int right = index + offset;

                        bool leftValid = left >= 0 && left <= lastValidIndex;
                        bool rightValid = right >= 0 && right <= lastValidIndex;

                        // If out of bounds in both directions, stop searching
                        if (!leftValid && !rightValid)
                        {
                            InformationBoxDialog.Show("Error", $"Unable to jump to image '{input}'. Image and neighboring images are not available.", MessageBoxButton.OK);
                            return;
                        }

                        if (leftValid && Images[left].IsEnabled)
                        {
                            index = left;
                            foundEnabled = true;
                            break;
                        }
                        if (rightValid && Images[right].IsEnabled)
                        {
                            index = right;
                            foundEnabled = true;
                            break;
                        }
                    }
                }

                if (foundEnabled)
                {
                    // Select the requested image
                    ImageListBox.SelectedIndex = index;
                }
            }
        }
        #endregion

        #region KeyBoard
        internal static bool IsControl => (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        internal static bool IsShift => (Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift;
        internal static bool IsAlt => (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt;

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // If serach box is active, return
            if (SearchBox.IsFocused || SearchBox.IsKeyboardFocusWithin || IsLoading) { return; }

            // Localize
            var destinationCount = DestinationCount;

            // Resets view options
            if (e.Key == Key.Escape) { ResetViewOptions(); }
            // Right rotation
            else if (e.Key == Key.Up) { RotateRight(); }
            // Left rotation
            else if (e.Key == Key.Down) { RotateLeft(); }
            // Previous image
            else if (e.Key == Key.Left) { PreviousImage(IsShift ? IsMarkedCondition : IsEnabledCondition); }
            // Next image
            else if (e.Key == Key.Right) { NextImage(IsShift ? IsMarkedCondition : IsEnabledCondition); }
            // First image
            else if (e.Key == Key.Home) { FirstImage(IsShift ? IsMarkedCondition : IsEnabledCondition); }
            // Last image
            else if (e.Key == Key.End) { LastImage(IsShift ? IsMarkedCondition : IsEnabledCondition); }
            // Previous image marked
            else if (e.Key == Key.A) { PreviousImage(IsMarkedCondition); }
            // Next image marked
            else if (e.Key == Key.D) { NextImage(IsMarkedCondition); }
            // Jump to image in split view
            else if (e.Key == Key.W) { FirstImage(x => x.IsInSplitView); }
            // Punsh into image
            else if (e.Key == Key.Space) { TogglePunshIn(IsControl); }
            // Jump to image
            else if (e.Key == Key.J) { JumpTo(); }
            // Mark image
            else if (e.Key == Key.M) { ToggleImageMark(); }
            // Toggle Thumbnail view
            else if (e.Key == Key.H) { ToggleThumbnailView(); }
            // Toggle Thumbnail view
            else if (e.Key == Key.L) { ToggleSideView(); }
            // Toggle search box
            else if (e.Key == Key.F && IsControl) { ToggleSearchBox(); }
            // Toggle sideview and thumbnails
            else if (e.Key == Key.F) 
            { 
                if (ToggleSideViewBtn.IsChecked == ToggleThumbnailViewBtn.IsChecked)
                {
                    ToggleSideView();
                    ToggleThumbnailView();
                }
                else
                {
                    ToggleSideViewBtn.IsChecked = false;
                    ToggleThumbnailViewBtn.IsChecked = false;
                }
            }
            // Toggle full resolution
            else if (e.Key == Key.R) { ToggleFullResolution(); }
            // Add destinations
            else if (e.Key == Key.D && IsControl) { AddFolderDestinations(); }
            // Undo
            else if (e.Key == Key.Z && IsControl) { Undo(); }
            // Open folder
            else if (e.Key == Key.O && (IsShift || IsControl)) { OpenFolder(recursive: IsShift, add: IsAlt); }
            // Reload folder
            else if (e.Key == Key.F5) { ReloadFolders(refresh: !IsShift, findRaws: IsAlt); }
            // Save image
            else if (e.Key == Key.S && IsControl) { SaveImage(move: IsShift); }
            // Activate split view
            else if (e.Key == Key.S && IsShift) { ActivateSplitView(CurrentImage); }
            // Toggle split view
            else if (e.Key == Key.S) { ToggleSplitView(); }
            // Add template
            else if (e.Key == Key.T && IsControl) { AddTemplate(); }
            // Delete image
            else if (e.Key == Key.Delete) { DeleteImage(dontRecycle: IsControl); }
            // Ignore Tabs
            else if (e.Key == Key.Tab) 
            { 
                // Wraps focus at last elemetn back to 0 (Image)
                if (Keyboard.FocusedElement is Control control && control.TabIndex == 22)
                {
                    Image.Focus();

                    // Mark as handled
                    e.Handled = true;
                }

                return; 
            }

            // Move to destination x
            else if ((e.Key == Key.D1 || e.Key == Key.NumPad1) && destinationCount >= 1) { Transfer(CurrentDestinations[0], MoveByDefault != IsShift); }
            else if ((e.Key == Key.D2 || e.Key == Key.NumPad2) && destinationCount >= 2) { Transfer(CurrentDestinations[1], MoveByDefault != IsShift); }
            else if ((e.Key == Key.D3 || e.Key == Key.NumPad3) && destinationCount >= 3) { Transfer(CurrentDestinations[2], MoveByDefault != IsShift); }
            else if ((e.Key == Key.D4 || e.Key == Key.NumPad4) && destinationCount >= 4) { Transfer(CurrentDestinations[3], MoveByDefault != IsShift); }
            else if ((e.Key == Key.D5 || e.Key == Key.NumPad5) && destinationCount >= 5) { Transfer(CurrentDestinations[4], MoveByDefault != IsShift); }
            else if ((e.Key == Key.D6 || e.Key == Key.NumPad6) && destinationCount >= 6) { Transfer(CurrentDestinations[5], MoveByDefault != IsShift); }
            else if ((e.Key == Key.D7 || e.Key == Key.NumPad7) && destinationCount >= 7) { Transfer(CurrentDestinations[6], MoveByDefault != IsShift); }
            else if ((e.Key == Key.D8 || e.Key == Key.NumPad8) && destinationCount >= 8) { Transfer(CurrentDestinations[7], MoveByDefault != IsShift); }
            else if ((e.Key == Key.D9 || e.Key == Key.NumPad9) && destinationCount >= 9) { Transfer(CurrentDestinations[8], MoveByDefault != IsShift); }

            // Mark event as handled
            e.Handled = true;
        }

        private void Windoe_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Catches space bar hits from buttons and forwards them to the window key down event
            if (e.Key == Key.Space)
            {
                Window_KeyDown(sender, e);
            }
        }

        private void ZoomSlider_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Reroute slider key presses
            Window_KeyDown(this, e);

            // Mark event as handled
            e.Handled = true;
        }

        private void ImageSlider_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Reroute slider key presses
            Window_KeyDown(this, e);

            // Mark event as handled
            e.Handled = true;
        }

        private void ThumbnailListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Reroute slider key presses
            Window_KeyDown(this, e);

            // Mark event as handled
            e.Handled = true;
        }

        private void ImageListBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Reroute slider key presses
            Window_KeyDown(this, e);

            // Mark event as handled
            e.Handled = true;
        }

        private void GridSplitter_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Reroute slider key presses
            Window_KeyDown(this, e);

            // Mark event as handled
            e.Handled = true;
        }

        private void MoveByDefaultBtn_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                // Toggles the checkbox
                MoveByDefaultBtn.IsChecked = !MoveByDefaultBtn.IsChecked;

                // Mark event as handled
                e.Handled = true;
            }
        }

        private void DestinationTemplateBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                DestinationTemplateBox.IsDropDownOpen = !DestinationTemplateBox.IsDropDownOpen;

                // Mark event as handled
                e.Handled = true;
            }
        }
        #endregion

        #region Mouse Clicks
        private void ThumbnailListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) => MouseRightClickContextMenu(sender as ListBox, e);

        private void ImageListBox_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) => MouseRightClickContextMenu(sender as ListBox, e);

        private static void MouseRightClickContextMenu(ListBox listBox, MouseButtonEventArgs e)
        {
            // Find the ListBoxItem directly using WPF's built-in helper
            if (e.OriginalSource is DependencyObject dep && ItemsControl.ContainerFromElement(listBox, dep) is ListBoxItem listBoxItem)
            {
                // Manually open the context menu since we blocked the default pipeline
                if (listBoxItem.ContextMenu is not null)
                {
                    listBoxItem.ContextMenu.PlacementTarget = listBoxItem;
                    listBoxItem.ContextMenu.IsOpen = true;
                }

                // Mark event as handled
                e.Handled = true;
            }
        }

        private void ImageListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) => MouseDoubleClickOpenImageInExplorer(sender as ListBox, e);

        private void ThumbnailListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e) => MouseDoubleClickOpenImageInExplorer(sender as ListBox, e);

        private static void MouseDoubleClickOpenImageInExplorer(ListBox listBox, MouseButtonEventArgs e)
        {
            // Find the ListBoxItem directly using WPF's built-in helper
            if (e.OriginalSource is DependencyObject dep && ItemsControl.ContainerFromElement(listBox, dep) is ListBoxItem listBoxItem)
            {
                // Get the item model
                if (listBox.ItemContainerGenerator.ItemFromContainer(listBoxItem) is ImageModel image)
                {
                    OpenImageInExplorer(image);

                    // Mark event as handled
                    e.Handled = true;
                }
            }
        }

        private void OpenImageInExplorer()
        {
            // If no image is available, return
            if (ImageCount == 0) { return; }

            OpenImageInExplorer(CurrentImage);
        }

        private static void OpenImageInExplorer(ImageModel image)
        {
            // Open and select image in file explorer
            Process.Start("explorer.exe", $"/select,\"{image.SourcePath}\"");
        }

        private void ThumbnailListBox_MouseDown(object sender, MouseButtonEventArgs e) => MiddleMouseButtonMark(sender as ListBox, e);

        private void ImageListBox_MouseDown(object sender, MouseButtonEventArgs e) => MiddleMouseButtonMark(sender as ListBox, e);

        private void MiddleMouseButtonMark(ListBox listBox, MouseButtonEventArgs e)
        {
            // Checks for middle mosue button
            if (e.ChangedButton != MouseButton.Middle) { return; }

            // Find the ListBoxItem directly using WPF's built-in helper
            if (e.OriginalSource is DependencyObject dep && ItemsControl.ContainerFromElement(listBox, dep) is ListBoxItem listBoxItem)
            {
                // Get the item model
                if (listBox.ItemContainerGenerator.ItemFromContainer(listBoxItem) is ImageModel image)
                {
                    ToggleImageMark(image);

                    // Mark event as handled
                    e.Handled = true;
                }
            }
        }

        private void Image_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Toggle image mark
            ToggleImageMark();

            // Mark event as handled
            e.Handled = true;
        }

        private void Image_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                // Open image in folder
                OpenImageInExplorer();

                // Mark event as handled
                e.Handled = true;
            }
        }

        private void SplitImage_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Toggle image mark
            ToggleImageMark(CurrentSplitViewImage);

            // Mark event as handled
            e.Handled = true;
        }

        private void SplitImage_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                // Open image in folder
                OpenImageInExplorer(CurrentSplitViewImage);

                // Mark event as handled
                e.Handled = true;
            }
        }

        private void SideViewGridSplitter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Resets the side view width
            SideViewColumn.Width = new(350);

            // Mark event as handled
            e.Handled = true;
        }

        private void DestinationViewSplitter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Resets the destination view width
            DestinationViewColumn.Width = new(300);

            // Mark event as handled
            e.Handled = true;
        }

        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                if (sender is TextBlock textBlock)
                {
                    // Gets the clicke item
                    var item = textBlock.DataContext;

                    // Check if item is a transfer
                    if (item is IImageTransfer transfer && transfer.TransferType != ImageTransferType.Delete)
                    {
                        // Open and select image in file explorer
                        Process.Start("explorer.exe", $"/select,\"{transfer.DestFile}\"");

                        // Mark event as handled
                        e.Handled = true;
                    }
                }
            }
        }
        #endregion

        #region Context Menu
        private void ContextMenuMarkItem_Click(object sender, RoutedEventArgs e)
        {
            // Cast the sender back to a image
            if (sender is FrameworkElement { DataContext: ImageModel image })
            {
                ToggleImageMark(image);

                // Mark event as handled
                e.Handled = true;
            }
        }

        private void ContextMenuUndoItem_Click(object sender, RoutedEventArgs e)
        {
            // Cast the sender back to a image
            if (sender is FrameworkElement { DataContext: ImageModel image })
            {
                if (image.IsTransfered)
                {
                    Undo(image.GetLastTransfer(), goToImage: false);
                }

                // Mark event as handled
                e.Handled = true;
            }
        }

        private void ContextMenuSplitViewItem_Click(object sender, RoutedEventArgs e)
        {
            // Cast the sender back to a image
            if (sender is FrameworkElement { DataContext: ImageModel image })
            {
                if (image.IsInSplitView)
                {
                    ToggleSplitView();
                }
                else
                {
                    ActivateSplitView(image);
                }

                // Mark event as handled
                e.Handled = true;
            }
        }

        private void ContextMenuOpenExplorerItem_Click(object sender, RoutedEventArgs e)
        {
            // Cast the sender back to a image
            if (sender is FrameworkElement { DataContext: ImageModel image })
            {
                OpenImageInExplorer(image);

                // Mark event as handled
                e.Handled = true;
            }
        }

        private void ContextMenuCopyToItem_Click(object sender, RoutedEventArgs e)
        {
            // Cast the sender back to a image
            if (sender is FrameworkElement { DataContext: ImageModel image })
            {
                SaveImage(image, goToNextImage: false);

                // Mark event as handled
                e.Handled = true;
            }
        }

        private void ContextMenuMoveToItem_Click(object sender, RoutedEventArgs e)
        {
            // Cast the sender back to a image
            if (sender is FrameworkElement { DataContext: ImageModel image })
            {
                SaveImage(image, move: true, goToNextImage: false);

                // Mark event as handled
                e.Handled = true;
            }
        }

        private void ContextMenuDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            // Cast the sender back to a image
            if (sender is FrameworkElement { DataContext: ImageModel image })
            {
                DeleteImage(image, goToNextImage: false);

                // Mark event as handled
                e.Handled = true;
            }
        }

        private void UndoEntryBtn_Click(object sender, RoutedEventArgs e)
        {
            // Cast the sender back to a transfer
            if (sender is FrameworkElement { DataContext: IImageTransfer transfer })
            {
                // Undo specified transfer
                Undo(transfer);
            }
        }
        #endregion

        #region SplitView
        private void ToggleSplitView()
        {
            // Show/Hide split view
            ToggleSplitViewBtn.IsChecked = !ToggleSplitViewBtn.IsChecked;
        }

        private bool _activatingSplitView = false;
        private void ToggleSplitViewBtn_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_activatingSplitView) { return; }

            if (ToggleSplitViewBtn.IsChecked == true)
            {
                // If no images are avialable, return
                if (ImageCount == 0)
                {
                    ToggleSplitViewBtn.IsChecked = false;
                    return;
                }
                
                ActivateSplitView(CurrentImage);
            }
            else if (ToggleSplitViewBtn.IsChecked == false)
            {
                // Hides the split view
                SplitViewColumn.MinWidth = 0;
                SplitViewColumn.Width = new(0);
                SplitViewSeperator.Width = new(0);
                SplitImage.Source = null;
                CurrentSplitViewImage?.IsInSplitView = false;
                CurrentSplitViewImage = null;
            }
        }

        private void ActivateSplitView(ImageModel image)
        {
            _activatingSplitView = true;

            // Set toggled to true
            ToggleSplitViewBtn.IsChecked = true;

            // Shows the split view
            SplitViewColumn.MinWidth = 100;
            SplitViewColumn.Width = new(5, GridUnitType.Star);
            SplitViewSeperator.Width = new(2);

            // Clear old split view image
            CurrentSplitViewImage?.IsInSplitView = false;

            // Load current image to split view
            SplitImage.Source = ReferenceEquals(image, CurrentImage) ? Image.Source : LoadImage(image);
            CurrentSplitViewImage = image;
            CurrentSplitViewImage.IsInSplitView = true;

            _activatingSplitView = false;
        }
        #endregion

        #region View Options
        private void ToggleThumbnailView()
        {
            // Show/Hide thumbnail list and image slider
            ToggleThumbnailViewBtn.IsChecked = !ToggleThumbnailViewBtn.IsChecked;

            if (ToggleThumbnailViewBtn.IsChecked == true)
            {
                LookAhead(_currentImageIndex);
            }
        }

        private void ToggleSideView()
        {
            // Show/hide side view
            ToggleSideViewBtn.IsChecked = !ToggleSideViewBtn.IsChecked;

            if (ToggleSideViewBtn.IsChecked == true)
            {
                LookAhead(_currentImageIndex);
            }
        }

        private void ToggleSideViewBtn_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (ToggleSideViewBtn.IsChecked == true)
            {
                // Shows the side view
                SideViewColumn.MinWidth = 350;
                SideViewColumn.Width = new(350);
                SideViewSplitterColumn.Width = new(3);
            }
            else if (ToggleSideViewBtn.IsChecked == false)
            {
                // Hides the side view
                SideViewColumn.MinWidth = 0;
                SideViewColumn.Width = new(0);
                SideViewSplitterColumn.Width = new(0);
            }
        }

        private void ToggleThumbnailViewBtn_IsCheckedChanged(object sender, RoutedEventArgs e)
        {
            if (ToggleThumbnailViewBtn.IsChecked == true)
            {
                // Shows the thumbnail view
                ThumbnailViewRow.Height = new(155);
            }
            else if (ToggleThumbnailViewBtn.IsChecked == false)
            {
                // Hides the thumbnail view
                ThumbnailViewRow.Height = new(0);
            }
        }

        private void ToggleFullResolution()
        {
            // Switch load quality
            LoadFullResolution = !LoadFullResolution;
        }

        private void FileNameLabel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var viewbox = sender as Viewbox;

            // Move label in circle pattern
            if (e.ChangedButton == MouseButton.Left)
            {
                if (viewbox.VerticalAlignment == VerticalAlignment.Top)
                {
                    if (viewbox.HorizontalAlignment == HorizontalAlignment.Right)
                    {
                        viewbox.VerticalAlignment = VerticalAlignment.Bottom;
                    }

                    viewbox.HorizontalAlignment = viewbox.HorizontalAlignment switch
                    {
                        HorizontalAlignment.Left => HorizontalAlignment.Center,
                        HorizontalAlignment.Center => HorizontalAlignment.Right,
                        HorizontalAlignment.Right => HorizontalAlignment.Right,
                        _ => HorizontalAlignment.Center,
                    };
                }
                else if (viewbox.VerticalAlignment == VerticalAlignment.Bottom)
                {
                    if (viewbox.HorizontalAlignment == HorizontalAlignment.Left)
                    {
                        viewbox.VerticalAlignment = VerticalAlignment.Top;
                    }

                    viewbox.HorizontalAlignment = viewbox.HorizontalAlignment switch
                    {
                        HorizontalAlignment.Left => HorizontalAlignment.Left,
                        HorizontalAlignment.Center => HorizontalAlignment.Left,
                        HorizontalAlignment.Right => HorizontalAlignment.Center,
                        _ => HorizontalAlignment.Center,
                    };
                }
            }
            // Switch up/down position
            else if (e.ChangedButton == MouseButton.Right || e.ChangedButton == MouseButton.Middle)
            {
                viewbox.VerticalAlignment = viewbox.VerticalAlignment == VerticalAlignment.Top ? VerticalAlignment.Bottom : VerticalAlignment.Top;
            }

            // Mark event as handled
            e.Handled = true;
        }

        private void ResetViewOptions()
        {
            ResetImageViewAngle();
            ResetImageZoomLevel();
            ResetImagePosition();
        }

        private void ResetImagePosition()
        {
            // Reset image position
            Image.TranslateX = 0;
            Image.TranslateY = 0;
        }

        private void ResetImageViewAngle()
        {
            // Resets the view angle
            ApplyViewAngle(0);
        }

        private void ResetImageZoomLevel()
        {
            // Resets the zoom level
            Image.ZoomRatio = 1;
        }
        #endregion

        #region Image Transfer
        private void Copy(IImageDestination destination)
        {
            // If no image is available, return
            if (ImageCount == 0) { return; }

            // Executes the transfer
            Transfer(destination, move: false);
        }

        private void Move(IImageDestination destination)
        {
            // If no image is available, return
            if (ImageCount == 0) { return; }

            // Executes the transfer
            Transfer(destination, move: true);
        }

        private void SaveImage(bool move = false, bool goToNextImage = true)
        {
            // If no image is available, return
            if (ImageCount == 0) { return; }

            SaveImage(CurrentImage, move: move, goToNextImage: goToNextImage);
        }

        private void SaveImage(ImageModel image, bool move = false, bool goToNextImage = true)
        {
            // Shows an save file dialog
            SaveFileDialog saveFileDialog = new()
            {
                Title = move ? $"Move '{image.UIFileName}' to" : $"Copy '{image.UIFileName}' to",
                FileName = image.UIFileName,
                CheckPathExists = true,
                OverwritePrompt = true,
                DefaultExt = Path.GetExtension(image.UIFileName)[1..]
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                // Transfers the file
                Transfer(new FileImageDestination(saveFileDialog.FileName, 0), move: move, includesNewName: true, goToNextImage: goToNextImage);
            }
        }

        private void DeleteImage(bool dontRecycle = false, bool goToNextImage = true)
        {
            // If no image is available, return
            if (ImageCount == 0) { return; }

            DeleteImage(CurrentImage, dontRecycle: dontRecycle, goToNextImage: goToNextImage);
        }

        private void DeleteImage(ImageModel image, bool dontRecycle = false, bool goToNextImage = true)
        {
            // Executes the transfer
            Transfer(new DeleteImageTransfer(image, dontRecycle), goToNextImage: goToNextImage);
        }

        private void Transfer(IImageDestination destination, bool move = false, bool includesNewName = false, bool goToNextImage = true)
        {
            // If no image is available, return
            if (ImageCount == 0) { return; }

            // Retrieves the current image
            var image = CurrentImage;

            // Creates a new file transfer
            IImageTransfer transfer = move ? new MoveImageTransfer(image, destination, includesNewName) : new CopyImageTransfer(image, destination, includesNewName);

            // Executes the transfer
            Transfer(transfer, goToNextImage: goToNextImage);
        }

        private void Transfer(IImageTransfer transfer, bool goToNextImage = true)
        {
            // Transfers the file
            if (transfer.Transfer())
            {
                // Add the transfer to the image
                transfer.Image.AddTransfer(transfer);

                // Unmark
                transfer.Image.IsMarked = false;

                // Saves the transfer
                Transfers.Add(transfer);

                if (goToNextImage)
                {
                    // Selects the next image
                    NextImage();
                }

                // Select last history entry
                HistoryList.SelectedItem = transfer;

                // Scroll to history entry
                HistoryList.ScrollIntoView(transfer);
            }
        }
        #endregion

        #region Undo
        private void UndoBtn_Click(object sender, RoutedEventArgs e) => Undo();

        private void Undo(bool goToImage = true)
        {
            // The last index
            var lastIndex = TransferCount - 1;

            // Checks if an undo action is available
            if (lastIndex < 0) { return; }

            // Executes the undo
            Undo(Transfers[lastIndex], goToImage: goToImage);
        }

        private void UndoAllBtn_Click(object sender, RoutedEventArgs e) => UndoAll();

        private async void UndoAll()
        {
            for (int i = TransferCount - 1; i >= 0; i--)
            {
                Undo(Transfers[i]);
            }
        }

        private void Undo(IImageTransfer transfer, bool goToImage = true)
        {
            // Undo the file transfer
            if (transfer.Undo())
            {
                // Selects the recovered image
                ImageListBox.SelectedItem = transfer.Image;


                if (goToImage)
                {
                    // Scrolls to the recovered image
                    ImageListBox.ScrollIntoView(transfer.Image);
                    ThumbnailListBox.ScrollIntoView(transfer.Image);
                }


                // Remove file transfer from undo list
                Transfers.Remove(transfer);

                // Select latest history entry, if available
                if (TransferCount > 0)
                {
                    var lastTransfer = Transfers[TransferCount - 1];

                    // Select last history entry
                    HistoryList.SelectedItem = lastTransfer;

                    // Scroll to history entry
                    HistoryList.ScrollIntoView(lastTransfer);
                }


                // Removes the transfer from the image
                transfer.Image.RemoveTransfer(transfer);

                // Removes the transfer object
                transfer = null;
            }
        }
        #endregion

        #region Drag&Drop
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            // Cheks for file drops
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Set drag and drop text
                StringBuilder textBuilder = new();

                if (IsAlt) { textBuilder.Append("[Alt] Add "); }
                else
                {
                    if (IsControl || IsShift) { textBuilder.Append(IsControl && IsShift ? "[Ctrl+Shift] " : IsControl ? "[Ctrl] " : IsShift ? "[Shift] " : string.Empty); }
                    textBuilder.Append(IsControl ? "Add " : "Load ");
                }
                textBuilder.Append(((string[])e.Data.GetData(DataFormats.FileDrop)).Length);
                textBuilder.Append(IsAlt ? " destination(s)" : " source(s)");
                if (!IsAlt)
                {
                    if (IsShift) { textBuilder.Append(" recursively"); }
                    if (!IsControl)
                    {
                        textBuilder.AppendLine();
                        textBuilder.Append("([Ctrl] to add)");
                    }
                    if (!IsShift)
                    {
                        textBuilder.AppendLine();
                        textBuilder.Append("([Shift] to load recursively)");
                    }
                    textBuilder.AppendLine();
                    textBuilder.Append("([Alt] to add destinations)");
                }

                DragandDropText.Text = textBuilder.ToString();


                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }

            // Mark event as handled
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            // Show the drag and drop label
            DragAndDropPopup.Visibility = Visibility.Collapsed;

            // Mark event as handled
            e.Handled = true;

            // Checks if dropped data are files/directorys
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Gest all dropped data
                string[] sourcePaths = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (sourcePaths is not null)
                {
                    // Alt: Add destinations
                    if (IsAlt)
                    {
                        foreach (var path in sourcePaths)
                        {
                            // If source is directory
                            if (Directory.Exists(path))
                            {
                                // Add diretory as destination
                                AddFolderDesitnation(new FolderImageDestination(path, CurrentDestinations.Count + 1));
                            }
                        }
                    }
                    // Add sources
                    else
                    {
                        // Load options
                        var add = IsControl;
                        var recursive = IsShift;
                        var findRaws = false;
                        var imageType = ImageType.All;
                        var orderType = ImageOrderType.LastWriteAsc;

                        // Prompt for loading options
                        var result = LoadingSourceOptionsDialog.Show(ref recursive, ref add, ref findRaws, ref imageType, ref orderType);

                        if (result == MessageBoxResult.OK)
                        {
                            // Loads the dropped folders
                            LoadSources(sourcePaths
                                .Where(x => Directory.Exists(x) || ValidFileTypes.Contains(Path.GetExtension(x), StringComparer.OrdinalIgnoreCase))
                                .Select<string, IImageSource>(x => Directory.Exists(x) 
                                                                            ? new FolderImageSource(x, recursive, imageType) 
                                                                            : new FileImageSource(x, imageType)),
                                    add: add, findRaws: findRaws, orderType: orderType);
                        }
                    }    
                }
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            // Cheks for file drops
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Show the drag and drop label
                DragAndDropPopup.Visibility = Visibility.Visible;
            }

            // Mark event as handled
            e.Handled = true;
        }

        private void Window_DragLeave(object sender, DragEventArgs e)
        {
            // Hide the drag and drop label
            DragAndDropPopup.Visibility = Visibility.Collapsed;

            // Mark event as handled
            e.Handled = true;
        }

        #endregion

        #region Search
        private readonly Func<ImageModel, string, bool> SearchFunc = (image, text) => image.UIShortPath.Contains(text, StringComparison.OrdinalIgnoreCase);

        internal void ToggleSearchBox()
        {
            if (IsSearchVisible)
            {
                // Hide search box
                IsSearchVisible = false;

                // Removes focus from searchbox
                Image.Focus();
            }
            else if (!IsSearchVisible)
            {
                // Show search box
                IsSearchVisible = true;

                // Sets focus to searchbox and selects the text
                SearchBox.Focus();
                SearchBox.SelectAll();
            }
        }

        private void SearchBoxBackdrop_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => ToggleSearchBox();

        private void SearhcBox_TextChanged(object sender, TextChangedEventArgs e) => Search();

        private void SearchNextBtn_Click(object sender, RoutedEventArgs e) => SearchNext();

        private void SearchPreviousBtn_Click(object sender, RoutedEventArgs e) => SearchPrevious();

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            // Mark event as handled
            e.Handled = true;

            // Toggle serach box
            if (e.Key == Key.Escape || (e.Key == Key.F && IsControl)) { ToggleSearchBox(); }
            // Search previous image
            else if ((e.Key == Key.Enter || e.Key == Key.Tab) && IsShift) { SearchPrevious(); }
            // Search next image
            else if (e.Key == Key.Enter || e.Key == Key.Tab) { SearchNext(); }
            // Do nothing
            else { e.Handled = false; }
        }

        private void Search()
        {
            _searchDebounceTimer.Stop();
            _searchDebounceTimer.Start();
        }

        private void SearchNext()
        {
            var currentIndex = _currentImageIndex;

            NextImage(x => SearchFunc(x, SearchBox.Text));

            // If a new was found
            if (currentIndex != _currentImageIndex)
            {
                SearchCounter.CurrentResultNumber++;
                OnPropertyChanged(nameof(SearchCounter));
            }
        }

        private void SearchPrevious()
        {
            var currentIndex = _currentImageIndex;

            PreviousImage(x => SearchFunc(x, SearchBox.Text));

            // If a new was found
            if (currentIndex != _currentImageIndex)
            {
                SearchCounter.CurrentResultNumber--;
                OnPropertyChanged(nameof(SearchCounter));
            }
        }
        #endregion

        #region Destination Controls
        private void CopyToDestinationBtn_Click(object sender, RoutedEventArgs e)
        {
            // Cast sender to FrameworkElement to get the button
            if (sender is FrameworkElement button && button.DataContext != null)
            {
                // Gets the actuial item
                if (button.DataContext is IImageDestination destination)
                {
                    Copy(destination);
                }
            }
        }

        private void MoveToDestinationBtn_Click(object sender, RoutedEventArgs e)
        {
            // Cast sender to FrameworkElement to get the button
            if (sender is FrameworkElement button && button.DataContext != null)
            {
                // Gets the actuial item
                if (button.DataContext is IImageDestination destination)
                {
                    Move(destination);
                }
            }
        }

        private void MoveDestinationUpBtn_Click(object sender, RoutedEventArgs e)
        {
            // Cast sender to FrameworkElement to get the button
            if (sender is FrameworkElement button && button.DataContext != null)
            {
                // Gets the actuial item
                var itemToMove = button.DataContext as IImageDestination;

                // Get index of the item
                int currentIndex = CurrentDestinations.IndexOf(itemToMove);

                // Check if already first element
                if (currentIndex > 0)
                {
                    // Move the item up in the ObservableCollection
                    CurrentDestinations.Move(currentIndex, currentIndex - 1);

                    // Update the numbering
                    CurrentDestinations[currentIndex].Number = currentIndex + 1;
                    CurrentDestinations[currentIndex - 1].Number = currentIndex;

                    // Scroll into view
                    DestinationListList.ScrollIntoView(itemToMove);

                    // Save destination templates
                    SaveDestinationTemplatesToDefault();
                }
            }
        }

        private void MoveDestinationDownBtn_Click(object sender, RoutedEventArgs e)
        {
            // Cast sender to FrameworkElement to get the button
            if (sender is FrameworkElement button && button.DataContext != null)
            {
                // Gets the actuial item
                var itemToMove = button.DataContext as IImageDestination;

                // Get index of the item
                int currentIndex = CurrentDestinations.IndexOf(itemToMove);

                // Check if already first element
                if (currentIndex < DestinationCount - 1)
                {
                    // Move the item up in the ObservableCollection
                    CurrentDestinations.Move(currentIndex, currentIndex + 1);

                    // Update the numbering
                    CurrentDestinations[currentIndex].Number = currentIndex + 1;
                    CurrentDestinations[currentIndex + 1].Number = currentIndex + 2;

                    // Scroll into view
                    DestinationListList.ScrollIntoView(itemToMove);

                    // Save destination templates
                    SaveDestinationTemplatesToDefault();
                }
            }
        }

        private void AddDestinationBtn_Click(object sender, RoutedEventArgs e) => AddFolderDestinations();

        private void EditDestinationBtn_Click(object sender, RoutedEventArgs e)
        {
            // Cast sender to FrameworkElement to get the button
            if (sender is FrameworkElement button && button.DataContext != null)
            {
                // Gets the actuial item
                if (button.DataContext is FolderImageDestination destination)
                {
                    // Shows an open folder dialog
                    OpenFolderDialog openFolderDialog = new()
                    {
                        Title = "Select destination",
                        DereferenceLinks = true,
                        Multiselect = false, // Dont allow to select multiple folders
                        InitialDirectory = destination.DestPath, // Opens the current destination directory
                    };

                    if (openFolderDialog.ShowDialog() == true)
                    {
                        // Sets the new destination
                        destination.DestPath = openFolderDialog.FolderName;

                        // Save destination templates
                        SaveDestinationTemplatesToDefault();
                    }
                }
            }
        }

        private void DeleteDestinationBtn_Click(object sender, RoutedEventArgs e)
        {
            // Cast sender to FrameworkElement to get the button
            if (sender is FrameworkElement button && button.DataContext != null)
            {
                // Gets the actuial item
                if (button.DataContext is IImageDestination destination)
                {
                    // confirmation prompt
                    var result = InformationBoxDialog.Show("Delete destination", $"Do you want to delete the destination '{destination.DestPath}'?", MessageBoxButton.YesNoCancel);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Removes the destination
                        CurrentDestinations.Remove(destination);

                        // Save destination templates
                        SaveDestinationTemplatesToDefault();
                    }
                }
            }
        }

        // Remebrs last location for destinations
        private string LastDestinationFolderPath = string.Empty;

        private void AddFolderDestinations()
        {
            // Shows an open folder dialog
            OpenFolderDialog openFolderDialog = new()
            {
                Title = "Select destination(s)",
                DereferenceLinks = true,
                Multiselect = true, // Allow to select multiple folders
                InitialDirectory = LastDestinationFolderPath // Opens the last added directory
            };

            if (openFolderDialog.ShowDialog() == true)
            {
                // Adds the destination
                foreach (var folder in openFolderDialog.FolderNames)
                {
                    AddFolderDesitnation(new(folder, CurrentDestinations.Count + 1));
                }

                if (openFolderDialog.FolderNames.Length > 0)
                {
                    // Sets the last opened directory
                    LastDestinationFolderPath = openFolderDialog.FolderNames[^1];
                }
            }
        }

        private void AddFolderDesitnation(FolderImageDestination destination)
        {
            // Adds the destination
            CurrentDestinations.Add(destination);

            // Scrolls to the new destination
            DestinationListList.ScrollIntoView(destination);

            if (!CurrentDestinationTemplate.IsDefault)
            {
                // Save destination templates
                SaveDestinationTemplatesToDefault();
            }            
        }

        private void TextBlock_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
            {
                // Cast sender to FrameworkElement to get the button
                if (sender is FrameworkElement button && button.DataContext != null)
                {
                    // Gets the actuial item
                    if (button.DataContext is FolderImageDestination destination)
                    {
                        // Open and select image in file explorer
                        Process.Start("explorer.exe", destination.DestPath);
                    }
                }
            }
        }

        private void SaveDestinationTemplatesToDefault() => SaveDestinationTemplates(_templateFilePath);

        private void SaveDestinationTemplates(string templatefilePath)
        {
            bool retry = false;
            do
            {
                try
                {
                    JsonSerialization.Save(DestinationTemplates.Where(x => !x.IsDefault), templatefilePath);
                }
                catch
                {
                    retry = InformationBoxDialog.Show("Error", $"Unable to save templates to '{templatefilePath}'", MessageBoxButton.RetryCancel) == MessageBoxResult.Retry;
                }

            } while (retry);
        }

        private void LoadDestinationTemplates(string templatefilePath)
        {
            // If the template file does not exists, return
            if (!File.Exists(templatefilePath)) { return; }

            bool retry = false;
            do
            {
                try
                {
                    // Load templates
                    foreach (var template in JsonSerialization.Load<ObservableCollection<DestinationTemplate>>(templatefilePath))
                    {
                        // Number destinations
                        var destCount = template.Destinations.Count;
                        for (int i = 0; i < destCount; i++)
                        {
                            template.Destinations[i].Number = i + 1;
                        }

                        // Add template
                        DestinationTemplates.Add(template);
                    }
                }
                catch
                {
                    retry = InformationBoxDialog.Show("Error", $"Unable to load templates from '{templatefilePath}'. Retry?", MessageBoxButton.RetryCancel) == MessageBoxResult.Retry;
                }

            } while (retry);
        }
        #endregion

        #region Destination Templates
        private void EditTemplateBtn_Click(object sender, RoutedEventArgs e)
        {
            // Cast sender to FrameworkElement to get the button
            if (sender is FrameworkElement button && button.DataContext != null)
            {
                // Gets the actuial item
                if (button.DataContext is DestinationTemplate template)
                {
                    // Can not edit default template
                    if (template.IsDefault) { return; }

                    // Edit name prompt
                    var result = TextEditDialog.Show("Edit template", "Template name:", template.Name, (x) => !string.IsNullOrEmpty(x), out var name);

                    if (result == MessageBoxResult.OK)
                    {
                        // Applys the new name
                        template.Name = name;

                        // Save destination templates
                        SaveDestinationTemplatesToDefault();
                    }
                }
            }
        }

        private void DeleteTemplateBtn_Click(object sender, RoutedEventArgs e)
        {
            // Cast sender to FrameworkElement to get the button
            if (sender is FrameworkElement button && button.DataContext != null)
            {
                // Gets the actuial item
                if (button.DataContext is DestinationTemplate template)
                {
                    // Can not delete default template
                    if (template.IsDefault) { return; }

                    // Confirmation prompt
                    var result = InformationBoxDialog.Show("Delete destination", $"Do you want to delete the template '{template.Name}'?", MessageBoxButton.YesNoCancel);

                    if (result == MessageBoxResult.Yes)
                    {
                        // Select default template when deleting current template
                        if (CurrentDestinationTemplate == template) { CurrentDestinationTemplate = DestinationTemplates[0]; }

                        // Removes the destination
                        DestinationTemplates.Remove(template);

                        // Save destination templates
                        SaveDestinationTemplatesToDefault();
                    }
                }
            }
        }

        private void AddTemplateBtn_Click(object sender, RoutedEventArgs e) => AddTemplate();

        private void AddTemplate()
        {
            // Enter name prompt
            var result = TextEditDialog.Show("New template", "Template name:", "New template", (x) => !string.IsNullOrEmpty(x), out var name);

            if (result == MessageBoxResult.OK)
            {
                /// Create new template
                var template = new DestinationTemplate(name);

                // Add template
                DestinationTemplates.Add(template);

                // Select new template
                CurrentDestinationTemplate = template;

                // Save destination templates
                SaveDestinationTemplatesToDefault();
            }       
        }
        #endregion

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Prevent first column from being growing and pushing out side of bounds
            SideViewColumn.MaxWidth = this.ActualWidth - 550;
        }
    }
}
#pragma warning restore CA1416 // Validate platform compatibility