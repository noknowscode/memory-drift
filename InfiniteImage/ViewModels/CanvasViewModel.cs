using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InfiniteImage.Models;
using InfiniteImage.Services;
using Windows.System;

namespace InfiniteImage.ViewModels;

public partial class CanvasViewModel : ObservableObject
{
    private const int MemoryCheckFrameInterval = 300;
    private const int HudUpdateFrameInterval = 3;

    private readonly ChunkService _chunkService;
    private readonly ProjectionService _projectionService;
    private readonly PerformanceTelemetry _telemetry;
    private readonly ImageCacheService _imageCacheService;
    private readonly PhotoLibraryService _photoLibraryService;

    private readonly HashSet<VirtualKey> _pressedKeys = [];
    private float _scrollAccumulator;

    private int _frameCount;
    private DateTime _lastFpsUpdate = DateTime.Now;

    private int _framesSinceMemoryCheck;
    private int _framesSinceHudUpdate;

    private int _lastCoordX = int.MinValue;
    private int _lastCoordY = int.MinValue;
    private int _lastCoordZ = int.MinValue;
    private int _lastChunkCount = int.MinValue;
    private int _lastPlaneCount = int.MinValue;
    private int _lastDepth = int.MinValue;
    private string _lastBackgroundYear = string.Empty;
    private string _lastCurrentDate = string.Empty;

    [ObservableProperty] private Camera _camera = new();
    [ObservableProperty] private List<ProjectedPlane> _visiblePlanes = [];
    [ObservableProperty] private int _fps;
    [ObservableProperty] private int _chunkCount;
    [ObservableProperty] private int _planeCount;
    [ObservableProperty] private double _viewportWidth = 800;
    [ObservableProperty] private double _viewportHeight = 600;
    [ObservableProperty] private string _coordX = "0";
    [ObservableProperty] private string _coordY = "0";
    [ObservableProperty] private string _coordZ = "0";
    [ObservableProperty] private string _fpsText = "60 FPS";
    [ObservableProperty] private string _chunkCountText = "0";
    [ObservableProperty] private string _planeCountText = "0";
    [ObservableProperty] private string _depthText = "0";
    [ObservableProperty] private double _speedBarWidth;
    [ObservableProperty] private double _motionGlowOpacity;
    [ObservableProperty] private string _currentDateText = string.Empty;
    [ObservableProperty] private string _backgroundYear = string.Empty;
    [ObservableProperty] private double _backgroundYearOpacity = 0.2;
    [ObservableProperty] private double _yearScale = 1.0;
    [ObservableProperty] private double _yearRotation = 0.0;
    [ObservableProperty] private double _yearOffsetX = 0.0;
    [ObservableProperty] private double _yearOffsetY = 0.0;
    [ObservableProperty] private bool _isLibraryMode;
    [ObservableProperty] private bool _isRandomMode = true;
    [ObservableProperty] private DateTimeOffset _earliestDate = DateTimeOffset.Now;
    [ObservableProperty] private DateTimeOffset _latestDate = DateTimeOffset.Now;
    [ObservableProperty] private float _timelineMaxZ = 1000f;
    [ObservableProperty] private float _currentTimelineZ;

    public CanvasViewModel(
        ChunkService chunkService,
        ProjectionService projectionService,
        PerformanceTelemetry telemetry,
        ImageCacheService imageCacheService,
        PhotoLibraryService photoLibraryService)
    {
        _chunkService = chunkService;
        _projectionService = projectionService;
        _telemetry = telemetry;
        _imageCacheService = imageCacheService;
        _photoLibraryService = photoLibraryService;
    }

    public ImageCacheService ImageCache => _imageCacheService;
    public PerformanceTelemetry Telemetry => _telemetry;

    public async Task LoadSavedLibraryAsync()
    {
        var library = await _photoLibraryService.LoadLibraryAsync();
        if (library is null) return;

        ApplyLibrary(library);
    }

    public async Task LoadPhotoLibraryAsync(Window? window = null)
    {
        var library = await _photoLibraryService.SelectAndScanFolderAsync(window);
        if (library is null || library.TotalPhotos == 0) return;

        _chunkService.ClearCache();
        ApplyLibrary(library);
    }

    private void ApplyLibrary(PhotoLibrary library)
    {
        IsLibraryMode = true;
        IsRandomMode = false;

        EarliestDate = library.EarliestDate;
        LatestDate = library.LatestDate;
        TimelineMaxZ = TimelineConfig.CalculateZForDate(library.LatestDate, library.EarliestDate);

        Camera.SetPosition(0, 0, 0);
    }

    public void SetTimelineZ(float z)
    {
        Camera.SetPosition(Camera.Position.X, Camera.Position.Y, z);
    }

    public void Update()
    {
        _telemetry.BeginFrame();

        ProcessKeyboardInput();
        ProcessScrollInput();

        Camera.Update();

        UpdateVisiblePlanes();

        _framesSinceHudUpdate++;
        if (_framesSinceHudUpdate >= HudUpdateFrameInterval)
        {
            _framesSinceHudUpdate = 0;
            UpdateHudProperties();
        }

        UpdateFps();
        CheckMemoryPressure();

        _telemetry.EndFrame();
    }

    private void ProcessKeyboardInput()
    {
        float dx = 0, dy = 0, dz = 0;

        if (_pressedKeys.Contains(VirtualKey.W) || _pressedKeys.Contains(VirtualKey.Up))
            dy -= CanvasConfig.KeyboardSpeedXY;
        if (_pressedKeys.Contains(VirtualKey.S) || _pressedKeys.Contains(VirtualKey.Down))
            dy += CanvasConfig.KeyboardSpeedXY;
        if (_pressedKeys.Contains(VirtualKey.A) || _pressedKeys.Contains(VirtualKey.Left))
            dx -= CanvasConfig.KeyboardSpeedXY;
        if (_pressedKeys.Contains(VirtualKey.D) || _pressedKeys.Contains(VirtualKey.Right))
            dx += CanvasConfig.KeyboardSpeedXY;

        if (_pressedKeys.Contains(VirtualKey.E) || _pressedKeys.Contains(VirtualKey.Space))
            dz += CanvasConfig.KeyboardSpeedZ;
        if (_pressedKeys.Contains(VirtualKey.Q) || _pressedKeys.Contains(VirtualKey.LeftShift) || _pressedKeys.Contains(VirtualKey.RightShift))
            dz -= CanvasConfig.KeyboardSpeedZ;

        if (dx != 0 || dy != 0 || dz != 0)
        {
            Camera.AddInput(dx, dy, dz);
        }
    }

    private void ProcessScrollInput()
    {
        if (Math.Abs(_scrollAccumulator) > 0.01f)
        {
            Camera.AddInput(0, 0, _scrollAccumulator);
            _scrollAccumulator *= 0.85f;
        }
    }

    private void UpdateVisiblePlanes()
    {
        var (cx, cy, cz) = Camera.ChunkCoords;
        var chunks = _chunkService.GetActiveChunks(cx, cy, cz);

        VisiblePlanes = _projectionService.ProjectPlanes(
            chunks, Camera, ViewportWidth, ViewportHeight, out var usedCache);

        ChunkCount = _chunkService.CachedChunkCount;
        PlaneCount = VisiblePlanes.Count;

        _telemetry.VisiblePlanes = PlaneCount;
        _telemetry.UsedCachedProjection = usedCache;
    }

    private void UpdateHudProperties()
    {
        var roundedX = (int)Camera.Position.X;
        var roundedY = (int)Camera.Position.Y;
        var roundedZ = (int)Camera.Position.Z;

        if (roundedX != _lastCoordX) { _lastCoordX = roundedX; CoordX = roundedX.ToString(); }
        if (roundedY != _lastCoordY) { _lastCoordY = roundedY; CoordY = roundedY.ToString(); }
        if (roundedZ != _lastCoordZ) { _lastCoordZ = roundedZ; CoordZ = roundedZ.ToString(); }
        if (ChunkCount != _lastChunkCount) { _lastChunkCount = ChunkCount; ChunkCountText = ChunkCount.ToString(); }
        if (PlaneCount != _lastPlaneCount) { _lastPlaneCount = PlaneCount; PlaneCountText = PlaneCount.ToString(); }
        if (roundedZ != _lastDepth) { _lastDepth = roundedZ; DepthText = roundedZ.ToString(); }

        var speed = Math.Min(Camera.Velocity.Length(), 100);
        SpeedBarWidth = speed * 2.4;

        var zSpeed = Math.Abs(Camera.Velocity.Z);
        MotionGlowOpacity = Math.Min(zSpeed / 50, 0.5);

        var library = _photoLibraryService.CurrentLibrary;
        if (library is { TotalPhotos: > 0 })
        {
            var date = TimelineConfig.CalculateDateForZ(Camera.Position.Z, library.EarliestDate);
            var yearStr = date.Year.ToString();

            if (yearStr != _lastBackgroundYear)
            {
                _lastBackgroundYear = yearStr;
                BackgroundYear = yearStr;
            }

            if (IsLibraryMode)
            {
                CurrentTimelineZ = Camera.Position.Z;
                if (yearStr != _lastCurrentDate)
                {
                    _lastCurrentDate = yearStr;
                    CurrentDateText = yearStr;
                }
            }

            var yearZSpeed = Math.Abs(Camera.Velocity.Z);

            var targetOpacity = yearZSpeed > 5 ? 0.35 : 0.18;
            BackgroundYearOpacity = BackgroundYearOpacity * 0.88 + targetOpacity * 0.12;

            var targetScale = Math.Min(1.0 + (yearZSpeed * 0.008), 1.15);
            YearScale = YearScale * 0.85 + targetScale * 0.15;

            var targetRotation = Math.Clamp(Camera.Velocity.X * -0.5, -3.0, 3.0);
            YearRotation = YearRotation * 0.92 + targetRotation * 0.08;

            var targetOffsetX = Math.Clamp(Camera.Velocity.X * -2.0, -40.0, 40.0);
            var targetOffsetY = Math.Clamp(Camera.Velocity.Y * -2.0, -40.0, 40.0);
            YearOffsetX = YearOffsetX * 0.9 + targetOffsetX * 0.1;
            YearOffsetY = YearOffsetY * 0.9 + targetOffsetY * 0.1;
        }
        else
        {
            BackgroundYearOpacity *= 0.85;
            YearScale = YearScale * 0.9 + 1.0 * 0.1;
            YearRotation *= 0.9;
            YearOffsetX *= 0.9;
            YearOffsetY *= 0.9;
        }
    }

    partial void OnCurrentTimelineZChanged(float value)
    {
        if (IsLibraryMode && Math.Abs(Camera.Position.Z - value) > 0.1f)
        {
            Camera.SetPosition(Camera.Position.X, Camera.Position.Y, value);
        }
    }

    private void UpdateFps()
    {
        _frameCount++;
        var now = DateTime.Now;
        if ((now - _lastFpsUpdate).TotalMilliseconds < 1000) return;

        Fps = _frameCount;
        FpsText = $"{Fps} FPS";

        _frameCount = 0;
        _lastFpsUpdate = now;

        _telemetry.CachedImages = _imageCacheService.CachedImageCount;
        _telemetry.ImageCacheMemoryBytes = _imageCacheService.TotalMemoryBytes;
    }

    private void CheckMemoryPressure()
    {
        _framesSinceMemoryCheck++;
        if (_framesSinceMemoryCheck < MemoryCheckFrameInterval) return;

        _framesSinceMemoryCheck = 0;

        if (_imageCacheService.TotalMemoryBytes > CanvasConfig.MaxImageCacheMemoryBytes * 0.8)
        {
            _ = Task.Run(_imageCacheService.OnMemoryPressure);
        }
    }

    public void OnKeyDown(VirtualKey key)
    {
        if (key == VirtualKey.R)
        {
            ResetCamera();
            return;
        }
        _pressedKeys.Add(key);
    }

    public void OnKeyUp(VirtualKey key)
    {
        _pressedKeys.Remove(key);
    }

    public void OnPan(double deltaX, double deltaY)
    {
        Camera.AddInput(
            -(float)deltaX * CanvasConfig.PanSensitivity,
            -(float)deltaY * CanvasConfig.PanSensitivity,
            0);
    }

    public void OnScroll(double deltaY)
    {
        _scrollAccumulator += (float)deltaY * CanvasConfig.ZoomSensitivity;
    }

    public void OnPinch(double scaleDelta)
    {
        var zDelta = (1 - scaleDelta) * 50;
        Camera.AddInput(0, 0, (float)zDelta);
    }

    [RelayCommand]
    private void ResetCamera()
    {
        Camera.Reset();
    }

    public void SetViewport(double width, double height)
    {
        ViewportWidth = width;
        ViewportHeight = height;
        _projectionService.MarkDirty();
    }
}
