using System.Numerics;

namespace InfiniteImage.Models;

public class Camera
{
    public Vector3 Position { get; set; } = Vector3.Zero;
    public Vector3 Velocity { get; set; } = Vector3.Zero;
    public Vector3 TargetVelocity { get; set; } = Vector3.Zero;

    private Vector3 _previousPosition = Vector3.Zero;

    public bool HasMoved { get; private set; }

    public bool IsActivelyMoving => Velocity.LengthSquared() > 100.0f;

    public (int cx, int cy, int cz) ChunkCoords => (
        (int)Math.Floor(Position.X / CanvasConfig.ChunkSize),
        (int)Math.Floor(Position.Y / CanvasConfig.ChunkSize),
        (int)Math.Floor(Position.Z / CanvasConfig.ChunkSize)
    );

    public void Update()
    {
        _previousPosition = Position;

        TargetVelocity *= CanvasConfig.VelocityDecay;
        Velocity = Vector3.Lerp(Velocity, TargetVelocity, CanvasConfig.VelocityLerp);
        Position += Velocity;

        HasMoved = Vector3.DistanceSquared(_previousPosition, Position) > 0.001f;
    }

    public void AddInput(float dx, float dy, float dz)
    {
        TargetVelocity += new Vector3(dx, dy, dz);
    }

    public void Reset()
    {
        Position = Vector3.Zero;
        Velocity = Vector3.Zero;
        TargetVelocity = Vector3.Zero;
        _previousPosition = Vector3.Zero;
        HasMoved = true;
    }

    public void SetPosition(float x, float y, float z)
    {
        Position = new Vector3(x, y, z);
        Velocity = Vector3.Zero;
        TargetVelocity = Vector3.Zero;
        _previousPosition = Position;
        HasMoved = true;
    }
}
