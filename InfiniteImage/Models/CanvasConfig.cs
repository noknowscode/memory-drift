namespace InfiniteImage.Models;

public static class CanvasConfig
{
    public const float ChunkSize = 600f;
    public const int RenderRadiusXY = 1;
    public const int RenderRadiusZ = 1;
    public const int PlanesPerChunk = 3;
    public const int MaxPhotosPerChunk = 4;
    public const int MaxVisiblePlanes = 60;
    public const int MaxCacheSize = 150;

    public const float Fov = 60f;
    public const float Near = 10f;
    public const float Far = 3000f;

    public const float VelocityLerp = 0.08f;
    public const float VelocityDecay = 0.94f;
    public const float PanSensitivity = 0.72f;
    public const float ZoomSensitivity = 2.25f;
    public const float KeyboardSpeedXY = 10.8f;
    public const float KeyboardSpeedZ = 18f;

    public const float PlaneMinSize = 120f;
    public const float PlaneMaxSize = 220f;
    public const float DepthFadeStart = 400f;
    public const float DepthFadeEnd = 1200f;
    public const float NearFadeDistance = 100f;
    public const float OpacityThreshold = 0.02f;

    public const int TargetFPS = 60;
    public const double TargetFrameTimeMs = 1000.0 / TargetFPS;
    public const int TelemetryUpdateIntervalMs = 1000;

    public const int MaxConcurrentImageLoads = 4;
    public const long MaxImageCacheMemoryMB = 50;
    public const long MaxImageCacheMemoryBytes = MaxImageCacheMemoryMB * 1024 * 1024;
    public const int EstimatedBytesPerPixel = 4;

    public static int TotalActiveChunks =>
        (2 * RenderRadiusXY + 1) * (2 * RenderRadiusXY + 1) * (2 * RenderRadiusZ + 1);

    public static int MaxPlanes => TotalActiveChunks * PlanesPerChunk;
}
