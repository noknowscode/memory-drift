using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Concurrent;
using InfiniteImage.Models;
using Microsoft.UI.Dispatching;

namespace InfiniteImage.Services;

public class ImageCacheService
{
    private readonly ConcurrentDictionary<string, CachedImage> _cache = new();
    private readonly LinkedList<string> _lruList = new();
    private readonly Dictionary<string, LinkedListNode<string>> _lruNodes = new();
    private readonly object _lruLock = new();
    private readonly HashSet<string> _loadingUrls = new();
    private long _totalMemoryBytes;
    private DispatcherQueue? _dispatcher;

    private readonly ILogger<ImageCacheService>? _logger;

    public ImageCacheService(ILogger<ImageCacheService>? logger = null)
    {
        _logger = logger;
    }

    public long TotalMemoryBytes => _totalMemoryBytes;
    public int CachedImageCount => _cache.Count;

    private sealed class CachedImage
    {
        public BitmapImage Image { get; set; } = null!;
        public long EstimatedBytes { get; set; }
    }

    public BitmapImage? GetOrCreateImage(string url, int decodePixelWidth, int decodePixelHeight, bool skipLoadIfNew = false)
    {
        _dispatcher ??= DispatcherQueue.GetForCurrentThread();

        if (_cache.TryGetValue(url, out var cached))
        {
            TouchLru(url);
            return cached.Image;
        }

        lock (_loadingUrls)
        {
            if (!_loadingUrls.Add(url))
            {
                return null;
            }
        }

        _ = _dispatcher.TryEnqueue(DispatcherQueuePriority.Low, async () =>
        {
            try
            {
                var estimatedBytes = (long)decodePixelWidth * decodePixelHeight * CanvasConfig.EstimatedBytesPerPixel;

                var image = new BitmapImage
                {
                    DecodePixelWidth = decodePixelWidth,
                    DecodePixelHeight = decodePixelHeight
                };

                if (IsLocalFilePath(url))
                {
                    await LoadLocalFileAsync(image, url);
                }
                else
                {
                    image.UriSource = new Uri(url);
                }

                var newCached = new CachedImage
                {
                    Image = image,
                    EstimatedBytes = estimatedBytes
                };

                _cache[url] = newCached;
                Interlocked.Add(ref _totalMemoryBytes, estimatedBytes);
                AddToLru(url);

                lock (_loadingUrls)
                {
                    _loadingUrls.Remove(url);
                }

                EvictIfOverBudget();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Image load failed for {Url}", url);
                lock (_loadingUrls)
                {
                    _loadingUrls.Remove(url);
                }
            }
        });

        return null;
    }

    private void EvictIfOverBudget()
    {
        while (_totalMemoryBytes > CanvasConfig.MaxImageCacheMemoryBytes && _cache.Count > 0)
        {
            string? keyToRemove = null;

            lock (_lruLock)
            {
                if (_lruList.First is { } first)
                {
                    keyToRemove = first.Value;
                    _lruList.RemoveFirst();
                    _lruNodes.Remove(keyToRemove);
                }
            }

            if (keyToRemove is null) break;
            RemoveFromCache(keyToRemove);
        }
    }

    private void RemoveFromCache(string key)
    {
        if (_cache.TryRemove(key, out var removed))
        {
            Interlocked.Add(ref _totalMemoryBytes, -removed.EstimatedBytes);
        }
    }

    private void AddToLru(string url)
    {
        lock (_lruLock)
        {
            var node = _lruList.AddLast(url);
            _lruNodes[url] = node;
        }
    }

    private void TouchLru(string url)
    {
        lock (_lruLock)
        {
            if (_lruNodes.TryGetValue(url, out var node))
            {
                _lruList.Remove(node);
                var newNode = _lruList.AddLast(url);
                _lruNodes[url] = newNode;
            }
        }
    }

    public void OnMemoryPressure()
    {
        var halfCount = _cache.Count / 2;
        var keysToRemove = new List<string>(halfCount);

        lock (_lruLock)
        {
            var node = _lruList.First;
            while (node is not null && keysToRemove.Count < halfCount)
            {
                keysToRemove.Add(node.Value);
                var next = node.Next;
                _lruList.Remove(node);
                _lruNodes.Remove(node.Value);
                node = next;
            }
        }

        foreach (var key in keysToRemove)
        {
            RemoveFromCache(key);
        }
    }

    public void Clear()
    {
        _cache.Clear();
        lock (_lruLock)
        {
            _lruList.Clear();
            _lruNodes.Clear();
        }
        _totalMemoryBytes = 0;
    }

    private static bool IsLocalFilePath(string url) =>
        url.StartsWith("file:///") || Path.IsPathRooted(url);

    private async Task LoadLocalFileAsync(BitmapImage image, string filePath)
    {
        try
        {
            var path = filePath.Replace("file:///", "");
            var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(path);
            using var stream = await file.OpenReadAsync();
            await image.SetSourceAsync(stream);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Local file load failed: {Path}", filePath);
        }
    }
}
