using System.Numerics;

namespace InfiniteImage.Models;

public class ImagePlane
{
    public string Id { get; set; } = string.Empty;

    public int ChunkX { get; set; }
    public int ChunkY { get; set; }
    public int ChunkZ { get; set; }

    public float LocalX { get; set; }
    public float LocalY { get; set; }
    public float LocalZ { get; set; }

    public float Width { get; set; }
    public float Height { get; set; }

    public float RotationX { get; set; }
    public float RotationY { get; set; }

    public float SinRotX { get; set; }
    public float CosRotX { get; set; }
    public float SinRotY { get; set; }
    public float CosRotY { get; set; }

    public int ImageIndex { get; set; }
    public int Hue { get; set; }
    public string? PhotoId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public int Year { get; set; }

    public Vector3 GetWorldPosition(int chunkX, int chunkY, int chunkZ) =>
        new(
            chunkX * CanvasConfig.ChunkSize + LocalX,
            chunkY * CanvasConfig.ChunkSize + LocalY,
            chunkZ * CanvasConfig.ChunkSize + LocalZ
        );

    public void CacheTrigValues()
    {
        var rotXRad = RotationX * MathF.PI / 180f;
        var rotYRad = RotationY * MathF.PI / 180f;
        SinRotX = MathF.Sin(rotXRad);
        CosRotX = MathF.Cos(rotXRad);
        SinRotY = MathF.Sin(rotYRad);
        CosRotY = MathF.Cos(rotYRad);
    }
}

public class ProjectedPlane
{
    public ImagePlane Source { get; set; } = null!;
    public double ScreenX { get; set; }
    public double ScreenY { get; set; }
    public double ScreenWidth { get; set; }
    public double ScreenHeight { get; set; }
    public double Depth { get; set; }
    public double Opacity { get; set; }
    public double Scale { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
}
