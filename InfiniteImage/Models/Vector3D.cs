using System.Numerics;

namespace InfiniteImage.Models;

/// <summary>
/// Vector3D extension methods for working with System.Numerics.Vector3.
/// </summary>
public static class Vector3DExtensions
{
    /// <summary>
    /// Linear interpolation between two vectors.
    /// </summary>
    public static Vector3 Lerp(this Vector3 from, Vector3 to, float amount)
    {
        return Vector3.Lerp(from, to, amount);
    }
}
