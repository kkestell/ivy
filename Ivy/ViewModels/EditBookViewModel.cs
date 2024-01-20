using System.Collections.ObjectModel;
using System.Web;
using System.Windows.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ivy.Common.Models;
using Ivy.Plugins.Abstract;
using Ivy.Services;
using Ivy.Services.Libraries;
using Microsoft.Extensions.DependencyInjection;

namespace Ivy.ViewModels;

public partial class EditBookViewModel : ViewModelBase
{
    private readonly IPluginHost _pluginHost;
    private readonly LibraryService _libraryService;
    private readonly WindowService _windowService;

    [ObservableProperty]
    private BookViewModel _book = new();

    public ObservableCollection<BookViewModel> Books { get; set; } = [];

    public ObservableCollection<MetadataSearchResultViewModel> SearchResults { get; set; } = [];
    
    public IEnumerable<string> Authors => _libraryService.Authors.Concat(Books.Select(x => x.Author)).Distinct();

    public IEnumerable<string> Series =>
        _libraryService.Series.Concat(Books.Select(x => x.Series)).Distinct().Where(x => x is not null)!;

    public ICommand BrowseForCoverCommand => new AsyncRelayCommand(BrowseForCover);

    public ICommand SearchGoogleForCoverCommand => new RelayCommand(SearchGoogleForCover);

    public ICommand SearchBingForCoverCommand => new RelayCommand(SearchBingForCover);
    
    public ICommand AutoMetadataCommand => new RelayCommand(AutoMetadata);
    
    public ICommand SearchCommand => new AsyncRelayCommand(UpdateSearchResults);

    public EditBookViewModel(WindowService windowService, LibraryService libraryService, IPluginHost pluginHost)
    {
        _windowService = windowService;
        _libraryService = libraryService;
        _pluginHost = pluginHost;
        
        Books.CollectionChanged += (_, _) =>
        {
            Book = Books.FirstOrDefault() ?? new BookViewModel();
            
            OnPropertyChanged(nameof(Books));
        };

        PropertyChanged += (_, args) =>
        {
            if (args.PropertyName != nameof(Book)) 
                return;
            
            SearchResults.Clear();

            OnPropertyChanged(nameof(Authors));
            OnPropertyChanged(nameof(Series));
        };
    }

    public EditBookViewModel() : this(
        DesignTimeServices.Services.GetRequiredService<WindowService>(),
        DesignTimeServices.Services.GetRequiredService<LibraryService>(),
        DesignTimeServices.Services.GetRequiredService<IPluginHost>())
    {
    }

    private async Task UpdateSearchResults()
    {
        SearchResults.Clear();

        List<MetadataSearchResult> results = [];
        foreach (var plugin in _pluginHost.MetadataPlugins)
        {
            var pluginResults = await plugin.Search(Book.Author, Book.Title);
            results.AddRange(pluginResults);
        }

        foreach (var result in results)
            SearchResults.Add(new MetadataSearchResultViewModel(result));
    }

    private async Task BrowseForCover()
    {
        Debug.Assert(Book is not null);
        
        var cover = await _windowService.ChooseOpenFile(
            "Choose cover",
            [new FilePickerFileType("Image Files") { Patterns = ["*.jpg", "*.jpeg", "*.png"] }]);

        if (cover is null)
            return;

        Book.Cover = new Bitmap(cover.Path.LocalPath);
        Book.CoverChanged = true;
    }

    private void SearchGoogleForCover()
    {
        Debug.Assert(Book is not null);
        
        var query = $"{Book.Title} {Book.Author} cover";
        var encodedQuery = HttpUtility.UrlEncode(query);
        var url = $"https://www.google.com/search?tbm=isch&q={encodedQuery}";

        WebSearch(url);
    }

    private void SearchBingForCover()
    {
        Debug.Assert(Book is not null);
        
        var query = $"{Book.Title} {Book.Author} cover";
        var encodedQuery = HttpUtility.UrlEncode(query);
        var url = $"https://www.bing.com/images/search?q={encodedQuery}";

        WebSearch(url);
    }
    
    private void AutoMetadata()
    {
        Debug.Assert(Book is not null);
        
        if (SearchResults.Count == 0)
            return;

        var title = SearchResults.GroupBy(x => x.Title)
            .OrderByDescending(x => x.Count())
            .Select(x => x.Key)
            .FirstOrDefault();
        
        var author = SearchResults.GroupBy(x => x.Author)
            .OrderByDescending(x => x.Count())
            .Select(x => x.Key)
            .FirstOrDefault();
        
        var description = SearchResults.Where(x => x.Title == title)
            .OrderByDescending(x => x.Description?.Length ?? 0)
            .Select(x => x.Description)
            .FirstOrDefault();
        
        var year = SearchResults.Where(x => x.FirstPublishedOn is not null)
            .OrderBy(x => x.FirstPublishedOn)
            .Select(x => x.FirstPublishedOn)
            .FirstOrDefault();
        
        if (!string.IsNullOrEmpty(title))
            Book.Title = title;
        
        if (!string.IsNullOrEmpty(author))
            Book.Author = author;
        
        if (!string.IsNullOrEmpty(description))
            Book.Description = description;
        
        if (year is not null)
            Book.Year = year;
    }

    private static void WebSearch(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
    }
}