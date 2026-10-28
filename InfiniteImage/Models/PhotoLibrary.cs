namespace InfiniteImage.Models;

public class PhotoLibrary
{
    public string FolderPath { get; set; } = string.Empty;
    public List<Photo> Photos { get; set; } = new();
    public DateTimeOffset EarliestDate { get; set; }
    public DateTimeOffset LatestDate { get; set; }
    public int TotalPhotos { get; set; }
}
