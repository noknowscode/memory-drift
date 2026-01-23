using InfiniteImage.Controls;
using InfiniteImage.Models;
using InfiniteImage.Services;
using InfiniteImage.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using System.Diagnostics;

namespace InfiniteImage;

public sealed partial class MainPage : Page
{
    private readonly CanvasViewModel _viewModel;
    private readonly Dictionary<string, ImagePlaneControl> _planeControls = new();
    private readonly Queue<ImagePlaneControl> _controlPool = new();
    private readonly HashSet<string> _visibleIdsCache = new();
    private readonly List<string> _toRemoveCache = new();

    private bool _isPointerPressed;
    private Point _lastPointerPosition;
    private DispatcherTimer? _renderTimer;

    public MainPage()
    {
        this.InitializeComponent();

        // Create services
        var telemetry = new PerformanceTelemetry();
        var imageCacheService = new ImageCacheService();
        var chunkService = new ChunkService();
        var projectionService = new ProjectionService();

        _viewModel = new CanvasViewModel(chunkService, projectionService, telemetry, imageCacheService);

        // Set DataContext for x:Bind
        this.DataContext = _viewModel;

        // Set up render loop
        this.Loaded += OnLoaded;
        this.Unloaded += OnUnloaded;
        this.SizeChanged += OnSizeChanged;

        // Focus for keyboard input
        this.IsTabStop = true;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Update viewport
        _viewModel.SetViewport(RootGrid.ActualWidth, RootGrid.ActualHeight);

        // Start render loop with frame rate limiter
        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(CanvasConfig.TargetFrameTimeMs)
        };
        _renderTimer.Tick += OnRenderTick;
        _renderTimer.Start();

        // Ensure keyboard focus
        this.Focus(FocusState.Programmatic);

        // Keep focus on page
        this.LostFocus += (s, args) => this.Focus(FocusState.Programmatic);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _renderTimer?.Stop();
        _renderTimer = null;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _viewModel.SetViewport(e.NewSize.Width, e.NewSize.Height);
    }

    private void OnRenderTick(object? sender, object e)
    {
        // Update simulation
        _viewModel.Update();

        // Update visuals
        UpdatePlaneControls();
    }

    private void UpdatePlaneControls()
    {
        var visiblePlanes = _viewModel.VisiblePlanes;
        var cameraMoving = _viewModel.Camera.IsActivelyMoving;

        // Reuse HashSet to avoid allocation
        _visibleIdsCache.Clear();

        foreach (var plane in visiblePlanes)
        {
            _visibleIdsCache.Add(plane.Source.Id);

            if (!_planeControls.TryGetValue(plane.Source.Id, out var control))
            {
                // Get from pool or create new
                control = GetOrCreateControl();
                _planeControls[plane.Source.Id] = control;
                Canvas3D.Children.Add(control);
            }

            // Always load images (no longer skip during movement)
            control.SetPlane(plane, _viewModel.ImageCache, false);
        }

        // Return unused controls to pool - avoid LINQ
        _toRemoveCache.Clear();
        foreach (var kvp in _planeControls)
        {
            if (!_visibleIdsCache.Contains(kvp.Key))
            {
                _toRemoveCache.Add(kvp.Key);
            }
        }

        foreach (var id in _toRemoveCache)
        {
            var control = _planeControls[id];
            Canvas3D.Children.Remove(control);
            _controlPool.Enqueue(control);
            _planeControls.Remove(id);
        }
    }

    private ImagePlaneControl GetOrCreateControl()
    {
        if (_controlPool.Count > 0)
        {
            return _controlPool.Dequeue();
        }
        return new ImagePlaneControl();
    }

    #region Input Handling

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        var key = e.Key.ToString();
        _viewModel.OnKeyDown(key);
        e.Handled = true;
    }

    private void OnKeyUp(object sender, KeyRoutedEventArgs e)
    {
        var key = e.Key.ToString();
        _viewModel.OnKeyUp(key);
        e.Handled = true;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isPointerPressed = true;
        _lastPointerPosition = e.GetCurrentPoint(Canvas3D).Position;
        Canvas3D.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPointerPressed) return;

        var currentPosition = e.GetCurrentPoint(Canvas3D).Position;
        var deltaX = currentPosition.X - _lastPointerPosition.X;
        var deltaY = currentPosition.Y - _lastPointerPosition.Y;

        _viewModel.OnPan(deltaX, deltaY);
        _lastPointerPosition = currentPosition;
        e.Handled = true;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isPointerPressed = false;
        Canvas3D.ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(Canvas3D);
        var delta = point.Properties.MouseWheelDelta;

        // Scroll down = fly forward (positive Z)
        _viewModel.OnScroll(-delta / 120.0 * 10);
        e.Handled = true;
    }

    private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        // Handle touch gestures
        var translation = e.Delta.Translation;
        _viewModel.OnPan(translation.X, translation.Y);

        // Pinch gesture
        if (Math.Abs(e.Delta.Scale - 1.0) > 0.001)
        {
            _viewModel.OnPinch(e.Delta.Scale);
        }

        e.Handled = true;
    }

    #endregion
}
