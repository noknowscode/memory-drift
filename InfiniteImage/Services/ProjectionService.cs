using InfiniteImage.Models;
using System.Numerics;

namespace InfiniteImage.Services;

/// <summary>
/// Handles 3D to 2D projection and culling with optimized caching.
/// </summary>
public class ProjectionService
{
    // Reusable collections to avoid per-frame allocations
    private readonly List<ProjectedPlane> _projectedPlanes = new(CanvasConfig.MaxPlanes);
    private readonly Dictionary<string, string> _urlCache = new();
    private readonly Dictionary<(int, int, int), string> _urlKeyCache = new();

    // Cached projection parameters
    private double _cachedViewportWidth;
    private double _cachedViewportHeight;
    private double _cachedFocalLength;
    private float _cachedFovRad;

    // Dirty flag tracking
    private Vector3 _lastCameraPosition;
    private bool _isDirty = true;

    /// <summary>
    /// Projects planes to screen space and returns only visible ones.
    /// Uses cached results if camera hasn't moved.
    /// </summary>
    public List<ProjectedPlane> ProjectPlanes(
        IEnumerable<Chunk> chunks,
        Camera camera,
        double viewportWidth,
        double viewportHeight,
        out bool usedCache)
    {
        // Check if we can use cached projection
        if (!_isDirty && !camera.HasMoved &&
            Math.Abs(_cachedViewportWidth - viewportWidth) < 0.1 &&
            Math.Abs(_cachedViewportHeight - viewportHeight) < 0.1)
        {
            usedCache = true;
            return _projectedPlanes;
        }

        usedCache = false;
        _isDirty = false;

        // Update cached viewport if changed
        if (Math.Abs(_cachedViewportWidth - viewportWidth) > 0.1 ||
            Math.Abs(_cachedViewportHeight - viewportHeight) > 0.1)
        {
            _cachedViewportWidth = viewportWidth;
            _cachedViewportHeight = viewportHeight;
            _cachedFovRad = CanvasConfig.Fov * MathF.PI / 180f;
            _cachedFocalLength = viewportHeight / (2 * Math.Tan(_cachedFovRad / 2));
        }

        _lastCameraPosition = camera.Position;

        // Clear and reuse existing list
        _projectedPlanes.Clear();

        foreach (var chunk in chunks)
        {
            foreach (var plane in chunk.Planes)
            {
                var worldPos = plane.GetWorldPosition(chunk.CX, chunk.CY, chunk.CZ);

                // Calculate relative position from camera
                var relX = worldPos.X - camera.Position.X;
                var relY = worldPos.Y - camera.Position.Y;
                var relZ = worldPos.Z - camera.Position.Z;

                // Depth culling
                if (relZ < CanvasConfig.Near || relZ > CanvasConfig.Far)
                    continue;

                // Calculate opacity from depth
                var opacity = CalculateOpacity(relZ);
                if (opacity < CanvasConfig.OpacityThreshold)
                    continue;

                // Perspective projection
                var scale = _cachedFocalLength / relZ;
                var screenX = viewportWidth / 2 + relX * scale;
                var screenY = viewportHeight / 2 + relY * scale;
                var screenWidth = plane.Width * scale;
                var screenHeight = plane.Height * scale;

                // Frustum culling
                var margin = Math.Max(screenWidth, screenHeight);
                if (screenX < -margin || screenX > viewportWidth + margin)
                    continue;
                if (screenY < -margin || screenY > viewportHeight + margin)
                    continue;

                // Get or generate cached image URL
                var imageUrl = GetOrCreateImageUrl(plane.ImageIndex, plane.Width, plane.Height);

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

        // Sort by depth only if we have planes (far to near for painter's algorithm)
        if (_projectedPlanes.Count > 1)
        {
            _projectedPlanes.Sort((a, b) => b.Depth.CompareTo(a.Depth));
        }

        return _projectedPlanes;
    }

    /// <summary>
    /// Gets or creates a cached image URL to avoid string allocations.
    /// Uses struct-based key to eliminate per-frame string allocations.
    /// </summary>
    private string GetOrCreateImageUrl(int imageIndex, float width, float height)
    {
        var picWidth = Math.Max(100, (int)(width * 1.5f));
        var picHeight = Math.Max(100, (int)(height * 1.5f));
        var structKey = (imageIndex, picWidth, picHeight);

        if (!_urlKeyCache.TryGetValue(structKey, out var url))
        {
            url = $"https://picsum.photos/seed/{imageIndex}/{picWidth}/{picHeight}";
            _urlKeyCache[structKey] = url;

            // Limit cache size (clear oldest 25% when over limit)
            if (_urlKeyCache.Count > 1000)
            {
                var count = 0;
                var toRemove = new List<(int, int, int)>(250);
                foreach (var key in _urlKeyCache.Keys)
                {
                    if (count++ >= 250) break;
                    toRemove.Add(key);
                }
                foreach (var k in toRemove)
                    _urlKeyCache.Remove(k);
            }
        }

        return url;
    }

    /// <summary>
    /// Calculates opacity based on depth with near and far fading.
    /// </summary>
    private static double CalculateOpacity(float relativeZ)
    {
        double opacity = 1.0;

        // Far fade
        if (relativeZ > CanvasConfig.DepthFadeStart)
        {
            var fadeRange = CanvasConfig.DepthFadeEnd - CanvasConfig.DepthFadeStart;
            opacity = Math.Max(0, 1 - (relativeZ - CanvasConfig.DepthFadeStart) / fadeRange);
        }

        // Near fade
        if (relativeZ < CanvasConfig.NearFadeDistance)
        {
            opacity *= relativeZ / CanvasConfig.NearFadeDistance;
        }

        return Math.Clamp(opacity, 0, 1);
    }

    /// <summary>
    /// Marks projection as dirty, forcing recomputation on next ProjectPlanes() call.
    /// </summary>
    public void MarkDirty()
    {
        _isDirty = true;
    }
}
