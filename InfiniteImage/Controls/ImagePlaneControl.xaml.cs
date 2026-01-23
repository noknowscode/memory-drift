using InfiniteImage.Models;
using InfiniteImage.Services;
using Microsoft.UI.Xaml.Media;

namespace InfiniteImage.Controls;

public sealed partial class ImagePlaneControl : UserControl
{
    private ProjectedPlane? _plane;
    private string? _currentImageUrl;
    private string? _lastArtistText;
    private readonly CompositeTransform _transform;

    public ImagePlaneControl()
    {
        this.InitializeComponent();

        // Reuse transform to avoid allocations
        _transform = new CompositeTransform();
        this.RenderTransform = _transform;
    }

    public void SetPlane(ProjectedPlane plane, ImageCacheService imageCache, bool cameraMoving = false)
    {
        _plane = plane;

        // Update text only if changed
        if (TitleText.Text != plane.Source.Title)
            TitleText.Text = plane.Source.Title;

        // Cache artist text to avoid repeated string formatting
        if (_lastArtistText == null || ArtistText.Text != _lastArtistText)
        {
            _lastArtistText = $"{plane.Source.Artist}, {plane.Source.Year}";
            ArtistText.Text = _lastArtistText;
        }

        // Update image only if URL changed
        if (_currentImageUrl != plane.ImageUrl)
        {
            _currentImageUrl = plane.ImageUrl;

            // Calculate small decode size for fast loading and low memory
            var decodeWidth = Math.Max(100, Math.Min(200, (int)plane.ScreenWidth));
            var decodeHeight = Math.Max(100, Math.Min(200, (int)plane.ScreenHeight));

            try
            {
                // Try to get cached or trigger background load
                var image = imageCache.GetOrCreateImage(plane.ImageUrl, decodeWidth, decodeHeight, skipLoadIfNew: false);

                if (image != null)
                {
                    // Image ready - display it
                    PlaneImage.Source = image;
                }
                else
                {
                    // Loading in background - show placeholder
                    SetPlaceholderColor(plane.Source.Hue);
                }
            }
            catch
            {
                SetPlaceholderColor(plane.Source.Hue);
            }
        }

        // Update transform
        UpdateTransform();
    }

    private void SetPlaceholderColor(int hue)
    {
        var (r, g, b) = HslToRgb(hue / 360.0, 0.65, 0.35);
        var innerGrid = RootGrid.Children[0] as Grid;
        if (innerGrid != null)
        {
            innerGrid.Background = new SolidColorBrush(
                Windows.UI.Color.FromArgb(255, (byte)r, (byte)g, (byte)b));
        }
    }

    private void UpdateTransform()
    {
        if (_plane == null) return;

        var translateX = _plane.ScreenX - _plane.ScreenWidth / 2;
        var translateY = _plane.ScreenY - _plane.ScreenHeight / 2;

        // Use cached sin/cos values
        var sinRotY = _plane.Source.SinRotY;
        var sinRotX = _plane.Source.SinRotX;

        _transform.TranslateX = translateX;
        _transform.TranslateY = translateY;
        _transform.ScaleX = 1 + sinRotY * 0.03;
        _transform.ScaleY = 1 + sinRotX * 0.03;
        _transform.SkewX = _plane.Source.RotationY * 0.12;
        _transform.SkewY = _plane.Source.RotationX * 0.08;

        this.Width = _plane.ScreenWidth;
        this.Height = _plane.ScreenHeight;
        this.Opacity = _plane.Opacity;

        Canvas.SetZIndex(this, (int)(10000 - _plane.Depth));
    }

    private static (int r, int g, int b) HslToRgb(double h, double s, double l)
    {
        double r, g, b;

        if (Math.Abs(s) < 0.001)
        {
            r = g = b = l;
        }
        else
        {
            var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            var p = 2 * l - q;
            r = HueToRgb(p, q, h + 1.0 / 3);
            g = HueToRgb(p, q, h);
            b = HueToRgb(p, q, h - 1.0 / 3);
        }

        return ((int)(r * 255), (int)(g * 255), (int)(b * 255));
    }

    private static double HueToRgb(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2) return q;
        if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }
}
