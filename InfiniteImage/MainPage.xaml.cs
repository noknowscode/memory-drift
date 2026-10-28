using InfiniteImage.Controls;
using InfiniteImage.Models;
using InfiniteImage.ViewModels;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

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

        _viewModel = App.Services?.GetRequiredService<CanvasViewModel>()
            ?? throw new InvalidOperationException("Host not initialized");

        this.DataContext = _viewModel;

        this.Loaded += OnLoaded;
        this.Unloaded += OnUnloaded;
        this.SizeChanged += OnSizeChanged;
        this.LostFocus += OnLostFocus;

        this.IsTabStop = true;
    }

    protected override async void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string mode && mode == "library")
        {
            await _viewModel.LoadPhotoLibraryAsync(App.CurrentWindow);
        }
        else
        {
            await _viewModel.LoadSavedLibraryAsync();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.SetViewport(RootGrid.ActualWidth, RootGrid.ActualHeight);

        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(CanvasConfig.TargetFrameTimeMs)
        };
        _renderTimer.Tick += OnRenderTick;
        _renderTimer.Start();

        this.Focus(FocusState.Programmatic);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_renderTimer is not null)
        {
            _renderTimer.Tick -= OnRenderTick;
            _renderTimer.Stop();
            _renderTimer = null;
        }

        this.Loaded -= OnLoaded;
        this.Unloaded -= OnUnloaded;
        this.SizeChanged -= OnSizeChanged;
        this.LostFocus -= OnLostFocus;
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        _viewModel.SetViewport(e.NewSize.Width, e.NewSize.Height);
    }

    private void OnLostFocus(object sender, RoutedEventArgs e)
    {
        this.Focus(FocusState.Programmatic);
    }

    private void OnRenderTick(object? sender, object e)
    {
        _viewModel.Update();
        UpdatePlaneControls();
    }

    private void UpdatePlaneControls()
    {
        var visiblePlanes = _viewModel.VisiblePlanes;

        _visibleIdsCache.Clear();

        foreach (var plane in visiblePlanes)
        {
            _visibleIdsCache.Add(plane.Source.Id);

            if (!_planeControls.TryGetValue(plane.Source.Id, out var control))
            {
                control = GetOrCreateControl();
                _planeControls[plane.Source.Id] = control;
                Canvas3D.Children.Add(control);
            }

            control.SetPlane(plane, _viewModel.ImageCache);
        }

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

    private ImagePlaneControl GetOrCreateControl() =>
        _controlPool.Count > 0 ? _controlPool.Dequeue() : new ImagePlaneControl();

    private void OnKeyDown(object sender, KeyRoutedEventArgs e)
    {
        _viewModel.OnKeyDown(e.Key);
        e.Handled = true;
    }

    private void OnKeyUp(object sender, KeyRoutedEventArgs e)
    {
        _viewModel.OnKeyUp(e.Key);
        e.Handled = true;
    }

    private void OnPointerPressed(object sender, PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(Canvas3D);
        if (!pointerPoint.PointerDevice.PointerDeviceType.Equals(PointerDeviceType.Mouse)) return;

        _isPointerPressed = true;
        _lastPointerPosition = pointerPoint.Position;
        Canvas3D.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void OnPointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isPointerPressed) return;

        var pointerPoint = e.GetCurrentPoint(Canvas3D);
        if (!pointerPoint.PointerDevice.PointerDeviceType.Equals(PointerDeviceType.Mouse)) return;

        var currentPosition = pointerPoint.Position;
        var deltaX = currentPosition.X - _lastPointerPosition.X;
        var deltaY = currentPosition.Y - _lastPointerPosition.Y;

        _viewModel.OnPan(deltaX, deltaY);
        _lastPointerPosition = currentPosition;
        e.Handled = true;
    }

    private void OnPointerReleased(object sender, PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(Canvas3D);
        if (!pointerPoint.PointerDevice.PointerDeviceType.Equals(PointerDeviceType.Mouse)) return;

        _isPointerPressed = false;
        Canvas3D.ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var point = e.GetCurrentPoint(Canvas3D);
        var delta = point.Properties.MouseWheelDelta;

        _viewModel.OnScroll(-delta / 120.0 * 10);
        e.Handled = true;
    }

    private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
    {
        var translation = e.Delta.Translation;
        if (Math.Abs(translation.X) > 0.1 || Math.Abs(translation.Y) > 0.1)
        {
            _viewModel.OnPan(translation.X * 0.5, translation.Y * 0.5);
        }

        if (Math.Abs(e.Delta.Scale - 1.0) > 0.001)
        {
            _viewModel.OnPinch(e.Delta.Scale);
        }

        e.Handled = true;
    }

    private async void OnUploadFolderClick(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadPhotoLibraryAsync(App.CurrentWindow);
    }
}
