using System.Text.Json;
using System.Text.RegularExpressions;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using InfiniteImage.Models;

namespace InfiniteImage.Services;

public class PhotoLibraryService
{
    private const string LibraryFileName = "photo-library.json";

    private static readonly string[] SupportedExtensions = [".jpg", ".jpeg", ".png", ".heic"];

    private static readonly Regex[] FilenameDateRegexes =
    [
        new(@"(\d{4})-(\d{2})-(\d{2})", RegexOptions.Compiled),
        new(@"(\d{4})(\d{2})(\d{2})", RegexOptions.Compiled),
        new(@"(\d{2})-(\d{2})-(\d{4})", RegexOptions.Compiled),
        new(@"(\d{2})(\d{2})(\d{4})", RegexOptions.Compiled),
    ];

    private readonly ILogger<PhotoLibraryService>? _logger;

    public PhotoLibraryService(ILogger<PhotoLibraryService>? logger = null)
    {
        _logger = logger;
    }

    public LibraryMode CurrentMode { get; private set; } = LibraryMode.Random;
    public PhotoLibrary? CurrentLibrary { get; private set; }

    public async Task<PhotoLibrary?> SelectAndScanFolderAsync(Window? window = null)
    {
        try
        {
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            folderPicker.FileTypeFilter.Add("*");

            if (window is not null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
            }

            var folder = await folderPicker.PickSingleFolderAsync();
            return folder is null ? null : await ScanFolderAsync(folder);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Folder pick or scan failed");
            return null;
        }
    }

    private async Task<PhotoLibrary?> ScanFolderAsync(StorageFolder folder)
    {
        try
        {
            var files = await GetImageFilesRecursiveAsync(folder);

            var photoResults = await Task.WhenAll(files.Select(BuildPhotoAsync));
            var photos = photoResults.Where(p => p is not null).Select(p => p!).ToList();

            if (photos.Count == 0)
            {
                return null;
            }

            photos.Sort((a, b) => a.DateTaken.CompareTo(b.DateTaken));

            var earliestDate = photos[0].DateTaken;
            var latestDate = photos[^1].DateTaken;

            foreach (var photo in photos)
            {
                photo.ZCoordinate = TimelineConfig.CalculateZForDate(photo.DateTaken, earliestDate);
            }

            var library = new PhotoLibrary
            {
                FolderPath = folder.Path,
                Photos = photos,
                EarliestDate = earliestDate,
                LatestDate = latestDate,
                TotalPhotos = photos.Count
            };

            await SaveLibraryAsync(library);

            CurrentLibrary = library;
            CurrentMode = LibraryMode.Personal;

            return library;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error scanning folder {Path}", folder.Path);
            return null;
        }
    }

    private async Task<Photo?> BuildPhotoAsync(StorageFile file)
    {
        try
        {
            var dateTaken = await ReadExifDateAsync(file)
                ?? TryParseDateFromFilename(file.Name)
                ?? (await file.GetBasicPropertiesAsync()).DateModified;

            int width = 800;
            int height = 600;
            try
            {
                var properties = await file.Properties.GetImagePropertiesAsync();
                if (properties.Width > 0) width = (int)properties.Width;
                if (properties.Height > 0) height = (int)properties.Height;
            }
            catch
            {
            }

            return new Photo
            {
                Id = Guid.NewGuid().ToString(),
                FilePath = file.Path,
                DateTaken = dateTaken,
                Title = Path.GetFileNameWithoutExtension(file.Name),
                Width = width,
                Height = height,
                ZCoordinate = 0
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error reading {File}", file.Name);
            return null;
        }
    }

    private async Task<List<StorageFile>> GetImageFilesRecursiveAsync(StorageFolder folder)
    {
        var imageFiles = new List<StorageFile>();

        try
        {
            foreach (var file in await folder.GetFilesAsync())
            {
                var extension = Path.GetExtension(file.Name).ToLowerInvariant();
                if (SupportedExtensions.Contains(extension))
                {
                    imageFiles.Add(file);
                }
            }

            foreach (var subfolder in await folder.GetFoldersAsync())
            {
                imageFiles.AddRange(await GetImageFilesRecursiveAsync(subfolder));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error reading folder {Path}", folder.Path);
        }

        return imageFiles;
    }

    private static async Task<DateTimeOffset?> ReadExifDateAsync(StorageFile file)
    {
        try
        {
            using var stream = await file.OpenReadAsync();
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var properties = await decoder.BitmapProperties.GetPropertiesAsync(
                new[] { "System.Photo.DateTaken" });

            if (properties.TryGetValue("System.Photo.DateTaken", out var dateProp)
                && dateProp.Value is not null
                && DateTimeOffset.TryParse(dateProp.Value.ToString(), out var date))
            {
                return date;
            }
        }
        catch
        {
        }

        return null;
    }

    private static DateTimeOffset? TryParseDateFromFilename(string filename)
    {
        try
        {
            foreach (var pattern in FilenameDateRegexes)
            {
                var match = pattern.Match(filename);
                if (!match.Success) continue;

                var yearFirst = match.Groups[1].Value.Length == 4;
                var iso = yearFirst
                    ? $"{match.Groups[1].Value}-{match.Groups[2].Value}-{match.Groups[3].Value}"
                    : $"{match.Groups[3].Value}-{match.Groups[2].Value}-{match.Groups[1].Value}";

                if (DateTime.TryParse(iso, out var date))
                {
                    return new DateTimeOffset(date);
                }
            }
        }
        catch
        {
        }

        return null;
    }

    public async Task SaveLibraryAsync(PhotoLibrary library)
    {
        try
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            var file = await localFolder.CreateFileAsync(LibraryFileName, CreationCollisionOption.ReplaceExisting);
            var json = JsonSerializer.Serialize(library, new JsonSerializerOptions { WriteIndented = true });
            await FileIO.WriteTextAsync(file, json);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error saving library");
        }
    }

    public async Task<PhotoLibrary?> LoadLibraryAsync()
    {
        try
        {
            var localFolder = ApplicationData.Current.LocalFolder;
            if (await localFolder.TryGetItemAsync(LibraryFileName) is not StorageFile file)
            {
                return null;
            }

            var json = await FileIO.ReadTextAsync(file);
            var library = JsonSerializer.Deserialize<PhotoLibrary>(json);

            if (library is { TotalPhotos: > 0 })
            {
                CurrentLibrary = library;
                CurrentMode = LibraryMode.Personal;
                return library;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error loading library");
        }

        return null;
    }

    public Photo? GetPhotoById(string photoId) =>
        CurrentLibrary?.Photos.FirstOrDefault(p => p.Id == photoId);

    public Photo? GetPhotoForCoordinate(float z)
    {
        if (CurrentLibrary is null || CurrentLibrary.Photos.Count == 0)
            return null;

        Photo? closest = null;
        var minDistance = float.MaxValue;
        foreach (var photo in CurrentLibrary.Photos)
        {
            var d = Math.Abs(photo.ZCoordinate - z);
            if (d < minDistance)
            {
                minDistance = d;
                closest = photo;
            }
        }
        return closest;
    }

    public List<Photo> GetPhotosInZRange(float zMin, float zMax)
    {
        if (CurrentLibrary is null)
            return [];

        var result = new List<Photo>();
        foreach (var photo in CurrentLibrary.Photos)
        {
            if (photo.ZCoordinate >= zMin && photo.ZCoordinate < zMax)
                result.Add(photo);
        }
        return result;
    }

    public void ClearLibrary()
    {
        CurrentLibrary = null;
        CurrentMode = LibraryMode.Random;
    }
}
