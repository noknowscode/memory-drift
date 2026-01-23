using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Concurrent;
using InfiniteImage.Models;
using Microsoft.UI.Dispatching;

namespace InfiniteImage.Services;

/// <summary>
/// Non-blocking image cache with efficient LRU eviction.
/// </summary>
public class ImageCacheService
{
    private readonly ConcurrentDictionary<string, CachedImage> _cache = new();
    private readonly LinkedList<string> _lruList = new();
    private readonly Dictionary<string, LinkedListNode<string>> _lruNodes = new();
    private readonly object _lruLock = new();
    private readonly HashSet<string> _loadingUrls = new();
    private long _totalMemoryBytes;
    private DispatcherQueue? _dispatcher;

    public long TotalMemoryBytes => _totalMemoryBytes;
    public int CachedImageCount => _cache.Count;

    private class CachedImage
    {
        public BitmapImage Image { get; set; } = null!;
        public long EstimatedBytes { get; set; }
    }

    /// <summary>
    /// Gets cached image or returns null and starts background load.
    /// NEVER blocks the UI thread.
    /// </summary>
    public BitmapImage? GetOrCreateImage(string url, int decodePixelWidth, int decodePixelHeight, bool skipLoadIfNew = false)
    {
        // Capture dispatcher on first call
        _dispatcher ??= DispatcherQueue.GetForCurrentThread();

        // Return cached image immediately
        if (_cache.TryGetValue(url, out var cached))
        {
            TouchLru(url);
            return cached.Image;
        }

        // Check if already loading
        lock (_loadingUrls)
        {
            if (_loadingUrls.Contains(url))
            {
                return null; // Loading in progress
            }
            _loadingUrls.Add(url);
        }

        // Start deferred load on UI thread with low priority
        _ = _dispatcher.TryEnqueue(DispatcherQueuePriority.Low, () =>
        {
            try
            {
                var estimatedBytes = (long)decodePixelWidth * decodePixelHeight * CanvasConfig.EstimatedBytesPerPixel;

                // Create image (will load asynchronously via UriSource)
                var image = new BitmapImage
                {
                    DecodePixelWidth = decodePixelWidth,
                    DecodePixelHeight = decodePixelHeight,
                    UriSource = new Uri(url)
                };

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

                // Evict if over budget
                EvictIfOverBudget();
            }
            catch
            {
                lock (_loadingUrls)
                {
                    _loadingUrls.Remove(url);
                }
            }
        });

        return null; // Return null immediately, image will appear when loaded
    }

    /// <summary>
    /// Efficient LRU-based eviction using O(1) operations.
    /// </summary>
    private void EvictIfOverBudget()
    {
        while (_totalMemoryBytes > CanvasConfig.MaxImageCacheMemoryBytes && _cache.Count > 0)
        {
            string? keyToRemove = null;

            lock (_lruLock)
            {
                if (_lruList.First != null)
                {
                    keyToRemove = _lruList.First.Value;
                    _lruList.RemoveFirst();
                    _lruNodes.Remove(keyToRemove);
                }
            }

            if (keyToRemove != null)
            {
                RemoveFromCache(keyToRemove);
            }
            else
            {
                break;
            }
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

    /// <summary>
    /// Handles memory pressure by clearing old cache entries using efficient LRU.
    /// </summary>
    public void OnMemoryPressure()
    {
        var halfCount = _cache.Count / 2;
        var keysToRemove = new List<string>(halfCount);

        lock (_lruLock)
        {
            var node = _lruList.First;
            while (node != null && keysToRemove.Count < halfCount)
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
}
