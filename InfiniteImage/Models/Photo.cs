namespace InfiniteImage.Models;

public class Photo
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string FilePath { get; set; } = string.Empty;
    public DateTimeOffset DateTaken { get; set; }
    public float ZCoordinate { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Width { get; set; }
    public int Height { get; set; }
}
