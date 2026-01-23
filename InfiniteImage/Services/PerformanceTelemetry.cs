using System.Diagnostics;
using InfiniteImage.Models;

namespace InfiniteImage.Services;

/// <summary>
/// Tracks performance metrics for the application.
/// </summary>
public class PerformanceTelemetry
{
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly Queue<double> _frameTimesMs = new();
    private const int MaxFrameTimeSamples = 120; // 2 seconds at 60 FPS

    private int _frameCount;
    private long _lastTelemetryUpdate;
    private double _lastFrameTime;

    public int Fps { get; private set; }
    public double AverageFrameTimeMs { get; private set; }
    public double MinFrameTimeMs { get; private set; } = double.MaxValue;
    public double MaxFrameTimeMs { get; private set; }
    public long TotalFrames { get; private set; }

    // Image cache statistics
    public int ImageCacheHits { get; set; }
    public int ImageCacheMisses { get; set; }
    public long ImageCacheMemoryBytes { get; set; }
    public int CachedImages { get; set; }

    // Projection statistics
    public int VisiblePlanes { get; set; }
    public int CulledPlanes { get; set; }
    public bool UsedCachedProjection { get; set; }

    /// <summary>
    /// Begins tracking a new frame.
    /// </summary>
    public void BeginFrame()
    {
        _lastFrameTime = _stopwatch.Elapsed.TotalMilliseconds;
    }

    /// <summary>
    /// Ends tracking a frame and updates statistics.
    /// </summary>
    public void EndFrame()
    {
        var currentTime = _stopwatch.Elapsed.TotalMilliseconds;
        var frameTime = currentTime - _lastFrameTime;

        _frameTimesMs.Enqueue(frameTime);
        if (_frameTimesMs.Count > MaxFrameTimeSamples)
            _frameTimesMs.Dequeue();

        _frameCount++;
        TotalFrames++;

        // Update min/max
        if (frameTime < MinFrameTimeMs) MinFrameTimeMs = frameTime;
        if (frameTime > MaxFrameTimeMs) MaxFrameTimeMs = frameTime;

        // Update FPS every second
        if (currentTime - _lastTelemetryUpdate >= CanvasConfig.TelemetryUpdateIntervalMs)
        {
            Fps = _frameCount;
            AverageFrameTimeMs = _frameTimesMs.Count > 0 ? _frameTimesMs.Average() : 0;

            _frameCount = 0;
            _lastTelemetryUpdate = (long)currentTime;
        }
    }

    /// <summary>
    /// Gets a formatted performance report.
    /// </summary>
    public string GetReport()
    {
        var cacheHitRate = ImageCacheHits + ImageCacheMisses > 0
            ? (100.0 * ImageCacheHits / (ImageCacheHits + ImageCacheMisses))
            : 0;

        return $"""
            FPS: {Fps}
            Avg Frame Time: {AverageFrameTimeMs:F2}ms
            Min/Max Frame Time: {MinFrameTimeMs:F2}ms / {MaxFrameTimeMs:F2}ms
            Total Frames: {TotalFrames}
            Visible Planes: {VisiblePlanes}
            Culled Planes: {CulledPlanes}
            Image Cache: {CachedImages} images, {ImageCacheMemoryBytes / 1024 / 1024}MB
            Cache Hit Rate: {cacheHitRate:F1}%
            Used Cached Projection: {UsedCachedProjection}
            """;
    }

    /// <summary>
    /// Resets telemetry counters.
    /// </summary>
    public void Reset()
    {
        _frameTimesMs.Clear();
        MinFrameTimeMs = double.MaxValue;
        MaxFrameTimeMs = 0;
        _frameCount = 0;
        _lastTelemetryUpdate = (long)_stopwatch.Elapsed.TotalMilliseconds;
    }
}
