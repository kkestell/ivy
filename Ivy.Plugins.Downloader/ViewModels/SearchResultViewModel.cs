using CommunityToolkit.Mvvm.ComponentModel;

namespace Ivy.Plugins.Downloader.ViewModels;

public partial class SearchResultViewModel : ObservableObject
{
    [ObservableProperty] private Guid _id = Guid.NewGuid();
    
    public List<string> Authors { get; set; }

    public string TruncatedAuthor
    {
        get
        {
            if (Authors.Count == 0)
                return "Unknown Author";
            
            var author = Authors[0];
            
            if (author.Length > 50)
                return author.Substring(0, 50).Trim() + "...";
            
            return author;
        }
    }
    
    public string Title { get; set; }
    
    public string TruncatedTitle => Title.Length > 50 ? Title.Substring(0, 50) + "..." : Title;
    
    public List<string> Urls { get; set; }
    
    public string? Isbn { get; set; }
    
    public string FileType { get; set; }
    
    public string Size { get; set; }
    
    public int Score { get; set; }
    
    public SearchResultViewModel(List<string> authors, string title, List<string> urls, string? isbn, string fileType, string size, int score)
    {
        Authors = authors;
        Title = title;
        Urls = urls;
        Isbn = isbn;
        FileType = fileType;
        Size = size;
        Score = score;
    }
}