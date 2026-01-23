using System.Numerics;

namespace InfiniteImage.Models;

/// <summary>
/// Represents the camera state in the 3D canvas.
/// </summary>
public class Camera
{
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 Velocity { get; set; } = Vector3.Zero;
    public Vector3 TargetVelocity { get; set; } = Vector3.Zero;

    private Vector3 _previousPosition = Vector3.Zero;

    /// <summary>
    /// Gets whether the camera has moved since last Update().
    /// </summary>
    public bool HasMoved { get; private set; }

    /// <summary>
    /// Gets whether the camera is actively moving fast (for image loading optimization).
    /// Only returns true during rapid movement to allow images to load during gentle navigation.
    /// </summary>
    public bool IsActivelyMoving => Velocity.LengthSquared() > 100.0f;

    /// <summary>
    /// Gets the current chunk coordinates based on camera position.
    /// </summary>
    public (int cx, int cy, int cz) ChunkCoords => (
        (int)Math.Floor(Position.X / CanvasConfig.ChunkSize),
        (int)Math.Floor(Position.Y / CanvasConfig.ChunkSize),
        (int)Math.Floor(Position.Z / CanvasConfig.ChunkSize)
    );

    /// <summary>
    /// Updates camera physics with velocity decay and lerp.
    /// </summary>
    public void Update()
    {
        _previousPosition = Position;

        // Decay target velocity
        TargetVelocity *= CanvasConfig.VelocityDecay;

        // Lerp actual velocity toward target
        Velocity = Velocity.Lerp(TargetVelocity, CanvasConfig.VelocityLerp);

        // Integrate position
        Position += Velocity;

        // Track if camera moved
        HasMoved = Vector3.DistanceSquared(_previousPosition, Position) > 0.001f;
    }

    /// <summary>
    /// Adds input to target velocity.
    /// </summary>
    public void AddInput(float dx, float dy, float dz)
    {
        TargetVelocity += new Vector3(dx, dy, dz);
    }

    /// <summary>
    /// Resets camera to origin.
    /// </summary>
    public void Reset()
    {
        Position = Vector3.Zero;
        Velocity = Vector3.Zero;
        TargetVelocity = Vector3.Zero;
        _previousPosition = Vector3.Zero;
        HasMoved = true;
    }
}
