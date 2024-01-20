namespace Ivy.Common.Models;

public class MetadataSearchResultCover
{
    public Uri? SmallThumbnail { get; set; }
    public Uri? Thumbnail { get; set; }
    public Uri? Small { get; set; }
    public Uri? Medium { get; set; }
    public Uri? Large { get; set; }
    public Uri? ExtraLarge { get; set; }
}

public abstract class MetadataSearchResult
{
    public string Identifier { get; set; }
    public abstract string IdentifierType { get; }
    public string? Isbn { get; set; }
    public string Title { get; set; }
    public List<string> Authors { get; set; }
    public int? PublishedOn { get; set; }
    public int? FirstPublishedOn { get; set; }
    public string? Language { get; set; }
    public string? Description { get; set; }
    public MetadataSearchResultCover? Cover { get; set; }
}