using System.Diagnostics;
using InfiniteImage.Models;

namespace InfiniteImage.Services;

public class PerformanceTelemetry
{
    private const int MaxFrameTimeSamples = 120;

    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private readonly Queue<double> _frameTimesMs = new();

    private int _frameCount;
    private long _lastTelemetryUpdate;
    private double _lastFrameTime;

    public int Fps { get; private set; }
    public double AverageFrameTimeMs { get; private set; }
    public double MinFrameTimeMs { get; private set; } = double.MaxValue;
    public double MaxFrameTimeMs { get; private set; }
    public long TotalFrames { get; private set; }

    public int ImageCacheHits { get; set; }
    public int ImageCacheMisses { get; set; }
    public long ImageCacheMemoryBytes { get; set; }
    public int CachedImages { get; set; }

    public int VisiblePlanes { get; set; }
    public int CulledPlanes { get; set; }
    public bool UsedCachedProjection { get; set; }

    public void BeginFrame()
    {
        _lastFrameTime = _stopwatch.Elapsed.TotalMilliseconds;
    }

    public void EndFrame()
    {
        var currentTime = _stopwatch.Elapsed.TotalMilliseconds;
        var frameTime = currentTime - _lastFrameTime;

        _frameTimesMs.Enqueue(frameTime);
        if (_frameTimesMs.Count > MaxFrameTimeSamples)
            _frameTimesMs.Dequeue();

        _frameCount++;
        TotalFrames++;

        if (frameTime < MinFrameTimeMs) MinFrameTimeMs = frameTime;
        if (frameTime > MaxFrameTimeMs) MaxFrameTimeMs = frameTime;

        if (currentTime - _lastTelemetryUpdate >= CanvasConfig.TelemetryUpdateIntervalMs)
        {
            Fps = _frameCount;
            AverageFrameTimeMs = _frameTimesMs.Count > 0 ? _frameTimesMs.Average() : 0;

            _frameCount = 0;
            _lastTelemetryUpdate = (long)currentTime;
        }
    }

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

    public void Reset()
    {
        _frameTimesMs.Clear();
        MinFrameTimeMs = double.MaxValue;
        MaxFrameTimeMs = 0;
        _frameCount = 0;
        _lastTelemetryUpdate = (long)_stopwatch.Elapsed.TotalMilliseconds;
    }
}
