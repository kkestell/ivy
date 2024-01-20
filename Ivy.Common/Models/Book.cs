namespace Ivy.Common.Models;

public class Book
{
    public int? Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string Author { get; set; } = string.Empty;
    
    public string? Series { get; set; }
    
    public int? SeriesNumber { get; set; }
    
    public string EpubPath { get; set; } = string.Empty;
    
    public bool HasCover { get; set; }
    
    public string? BookType { get; set; }
    
    public int? Year { get; set; }
    
    public string? Description { get; set; }
    
    public DateTime AddedOn { get; set; }

    public string? CoverPath
    {
        get
        {
            if (!HasCover)
                return null;
            
            var epubDirectory = new DirectoryInfo(Path.GetDirectoryName(EpubPath)!);
            var coverPath = Path.Combine(epubDirectory.FullName, "cover.jpg");
            
            return !File.Exists(coverPath) ? null : coverPath;
        }
    }
    
    public string? ThumbnailPath
    {
        get
        {
            if (!HasCover)
                return null;
            
            var epubDirectory = new DirectoryInfo(Path.GetDirectoryName(EpubPath)!);
            var thumbnailPath = Path.Combine(epubDirectory.FullName, "thumb.jpg");
            
            return !File.Exists(thumbnailPath) ? null : thumbnailPath;
        }
    }
}