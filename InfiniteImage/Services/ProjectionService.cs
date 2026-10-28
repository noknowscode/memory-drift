using InfiniteImage.Models;
using System.Numerics;

namespace InfiniteImage.Services;

public class ProjectionService
{
    private readonly List<ProjectedPlane> _projectedPlanes = new(CanvasConfig.MaxPlanes);
    private readonly Dictionary<(int, int, int), string> _urlKeyCache = new();
    private readonly PhotoLibraryService _libraryService;

    private double _cachedViewportWidth;
    private double _cachedViewportHeight;
    private double _cachedFocalLength;
    private float _cachedFovRad;

    private bool _isDirty = true;

    public ProjectionService(PhotoLibraryService libraryService)
    {
        _libraryService = libraryService;
    }

    public List<ProjectedPlane> ProjectPlanes(
        IEnumerable<Chunk> chunks,
        Camera camera,
        double viewportWidth,
        double viewportHeight,
        out bool usedCache)
    {
        if (!_isDirty && !camera.HasMoved &&
            Math.Abs(_cachedViewportWidth - viewportWidth) < 0.1 &&
            Math.Abs(_cachedViewportHeight - viewportHeight) < 0.1)
        {
            usedCache = true;
            return _projectedPlanes;
        }

        usedCache = false;
        _isDirty = false;

        if (Math.Abs(_cachedViewportWidth - viewportWidth) > 0.1 ||
            Math.Abs(_cachedViewportHeight - viewportHeight) > 0.1)
        {
            _cachedViewportWidth = viewportWidth;
            _cachedViewportHeight = viewportHeight;
            _cachedFovRad = CanvasConfig.Fov * MathF.PI / 180f;
            _cachedFocalLength = viewportHeight / (2 * Math.Tan(_cachedFovRad / 2));
        }

        _projectedPlanes.Clear();

        foreach (var chunk in chunks)
        {
            foreach (var plane in chunk.Planes)
            {
                var worldPos = plane.GetWorldPosition(chunk.CX, chunk.CY, chunk.CZ);

                var relX = worldPos.X - camera.Position.X;
                var relY = worldPos.Y - camera.Position.Y;
                var relZ = worldPos.Z - camera.Position.Z;

                if (relZ < CanvasConfig.Near || relZ > CanvasConfig.Far)
                    continue;

                var opacity = CalculateOpacity(relZ);
                if (opacity < CanvasConfig.OpacityThreshold)
                    continue;

                var scale = _cachedFocalLength / relZ;
                var screenX = viewportWidth / 2 + relX * scale;
                var screenY = viewportHeight / 2 + relY * scale;
                var screenWidth = plane.Width * scale;
                var screenHeight = plane.Height * scale;

                var margin = Math.Max(screenWidth, screenHeight);
                if (screenX < -margin || screenX > viewportWidth + margin)
                    continue;
                if (screenY < -margin || screenY > viewportHeight + margin)
                    continue;

                var imageUrl = GetOrCreateImageUrl(plane);

                _projectedPlanes.Add(new ProjectedPlane
                {
                    Source = plane,
                    ScreenX = screenX,
                    ScreenY = screenY,
                    ScreenWidth = screenWidth,
                    ScreenHeight = screenHeight,
                    Depth = relZ,
                    Opacity = opacity,
                    Scale = scale,
                    ImageUrl = imageUrl
                });
            }
        }

        if (_projectedPlanes.Count > 1)
        {
            _projectedPlanes.Sort(static (a, b) => b.Depth.CompareTo(a.Depth));
        }

        if (_projectedPlanes.Count > CanvasConfig.MaxVisiblePlanes)
        {
            _projectedPlanes.RemoveRange(0, _projectedPlanes.Count - CanvasConfig.MaxVisiblePlanes);
        }

        return _projectedPlanes;
    }

    private string GetOrCreateImageUrl(ImagePlane plane)
    {
        if (!string.IsNullOrEmpty(plane.PhotoId))
        {
            var photo = _libraryService.GetPhotoById(plane.PhotoId);
            return photo?.FilePath ?? string.Empty;
        }

        var picWidth = Math.Max(100, (int)(plane.Width * 1.5f));
        var picHeight = Math.Max(100, (int)(plane.Height * 1.5f));
        var structKey = (plane.ImageIndex, picWidth, picHeight);

        if (!_urlKeyCache.TryGetValue(structKey, out var url))
        {
            url = $"https://picsum.photos/seed/{plane.ImageIndex}/{picWidth}/{picHeight}";
            _urlKeyCache[structKey] = url;

            if (_urlKeyCache.Count > 1000)
            {
                var toRemove = new List<(int, int, int)>(250);
                foreach (var key in _urlKeyCache.Keys)
                {
                    if (toRemove.Count >= 250) break;
                    toRemove.Add(key);
                }
                foreach (var k in toRemove)
                    _urlKeyCache.Remove(k);
            }
        }

        return url;
    }

    private static double CalculateOpacity(float relativeZ)
    {
        double opacity = 1.0;

        if (relativeZ > CanvasConfig.DepthFadeStart)
        {
            var fadeRange = CanvasConfig.DepthFadeEnd - CanvasConfig.DepthFadeStart;
            opacity = Math.Max(0, 1 - (relativeZ - CanvasConfig.DepthFadeStart) / fadeRange);
        }

        if (relativeZ < CanvasConfig.NearFadeDistance)
        {
            opacity *= relativeZ / CanvasConfig.NearFadeDistance;
        }

        return Math.Clamp(opacity, 0, 1);
    }

    public void MarkDirty()
    {
        _isDirty = true;
    }
}
