namespace InfiniteImage.Models;

/// <summary>
/// Represents a 3D chunk containing image planes.
/// </summary>
public class Chunk
{
    public int CX { get; }
    public int CY { get; }
    public int CZ { get; }
    public string Key => $"{CX},{CY},{CZ}";
    public List<ImagePlane> Planes { get; }

    public Chunk(int cx, int cy, int cz, List<ImagePlane> planes)
    {
        CX = cx;
        CY = cy;
        CZ = cz;
        Planes = planes;
    }
}
