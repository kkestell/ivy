using System.Text.Json;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Ivy.Common;
using Ivy.Common.Models;

namespace Ivy.Services.Libraries;

public class LibraryService : IDisposable
{
    private const string StateFilePath = "libraryState.json";

    private readonly List<LibraryServiceCollection> _libraries = [];

    private LibraryServiceCollection? _selectedLibrary;

    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    
    /// <summary>
    /// 
    /// </summary>
    public IEnumerable<string> Authors => _selectedLibrary?.DatabaseService.GetAuthors() ?? new List<string>();
    
    /// <summary>
    /// 
    /// </summary>
    public IEnumerable<string> Series => _selectedLibrary?.DatabaseService.GetSeries() ?? new List<string>();
    
    /// <summary>
    /// 
    /// </summary>
    public List<Book> SelectedBooks { get; set; } = [];
    
    /// <summary>
    /// Gets a collection of all libraries.
    /// </summary>
    public IEnumerable<Library> Libraries => _libraries.Select(x => x.Library);

    /// <summary>
    /// Gets or sets the currently selected library.
    /// Setting this property to a library not in the collection throws a KeyNotFoundException.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the specified library is not found in the collection.</exception>
    public Library? SelectedLibrary
    {
        get => _selectedLibrary?.Library;
        set
        {
            if (value is not null && _libraries.All(x => x.Library != value))
                throw new KeyNotFoundException($"Library {value.Name} not found");

            _selectedLibrary = _libraries.FirstOrDefault(x => x.Library == value);

            if (_selectedLibrary is not null)
            {
                _selectedLibrary.Library.Books = _selectedLibrary.DatabaseService.GetBooks().ToList();
            }
            
            LibrarySelected?.Invoke(this, value);
        }
    }

    /// <summary>
    /// Occurs when a library is selected.
    /// </summary>
    public event EventHandler<Library?>? LibrarySelected;

    /// <summary>
    /// Occurs when a new library is added.
    /// </summary>
    public event EventHandler<Library?>? LibraryAdded;

    /// <summary>
    /// Occurs when a new book is imported into the selected library.
    /// </summary>
    public event EventHandler<Book>? BookImported;
    
    /// <summary>
    /// Occurs when a book is deleted.
    /// </summary>
    public event EventHandler<Book>? BookDeleted;

    /// <summary>
    /// Initializes a new instance of the LibraryService class and loads the library state.
    /// </summary>
    public LibraryService()
    {
        LoadState();
    }

    /// <summary>
    /// Adds a new library to the service and selects it.
    /// </summary>
    /// <param name="library">The library to add.</param>
    public void AddLibrary(Library library)
    {
        _libraries.Add(new LibraryServiceCollection(library));
        SelectedLibrary = library;
        
        SaveState();

        LibraryAdded?.Invoke(this, library);
    }

    /// <summary>
    /// Imports a book into the currently selected library.
    /// </summary>
    /// <param name="epubFileInfo">File information for the book's EPUB file.</param>
    /// <exception cref="InvalidOperationException">Thrown when no library is currently selected.</exception>
    public void ImportBook(FileInfo epubFileInfo)
    {
        if (_selectedLibrary is null)
            throw new InvalidOperationException("No library selected");

        var epub = new Epub(epubFileInfo.FullName);

        var title = epub.Title?.Trim();
        if (string.IsNullOrEmpty(title))
            title = Path.GetFileNameWithoutExtension(epubFileInfo.Name);

        var author = epub.Creators.FirstOrDefault()?.Name.Trim();
        if (string.IsNullOrEmpty(author))
            author = "Unknown Author";

        var description = epub.Description?.Trim();
        if (!string.IsNullOrEmpty(description))
        {
            var converter = new ReverseMarkdown.Converter();
            description = converter.Convert(description);
        }

        var book = new Book
        {
            Title = title,
            Author = author,
            BookType = epub.Type,
            Year = epub.Year,
            Series = epub.Series,
            SeriesNumber = epub.SeriesNumber,
            EpubPath = epubFileInfo.FullName,
            Description = description
        };

        var idx = 1;
        while (_selectedLibrary.Library.ContainsBook(book))
        {
            book.Title = $"{title} ({idx++})";
        }        
        _selectedLibrary.FileService.ImportBook(book, epubFileInfo, epub.CoverFileInfo);
        _selectedLibrary.DatabaseService.ImportBook(book);
        
        _selectedLibrary.Library.Books.Add(book);

        Dispatcher.UIThread.InvokeAsync(() => BookImported?.Invoke(this, book));
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="book"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void UpdateBook(Book book)
    {
        if (_selectedLibrary is null)
            throw new InvalidOperationException("No library selected");
        
        _selectedLibrary.FileService.UpdateBook(book);
        _selectedLibrary.DatabaseService.UpdateBook(book);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="book"></param>
    /// <param name="newCover"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void UpdateCover(Book book, Bitmap newCover)
    {
        if (_selectedLibrary is null)
            throw new InvalidOperationException("No library selected");
        
        _selectedLibrary.FileService.UpdateCover(book, newCover);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="book"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public void DeleteBook(Book book)
    {
        if (_selectedLibrary is null)
            throw new InvalidOperationException("No library selected");
        
        _selectedLibrary.FileService.DeleteBook(book);
        _selectedLibrary.DatabaseService.DeleteBook(book);
        
        _selectedLibrary.Library.Books.Remove(book);
        
        Dispatcher.UIThread.InvokeAsync(() => BookDeleted?.Invoke(this, book));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="book"></param>
    public static void SyncMetadata(Book book)
    {
        var epub = new Epub(book.EpubPath);
        
        epub.Title = book.Title;
        epub.Creators = [new Creator { Name = book.Author, FileAs = Epub.AuthorNameToFileAs(book.Author), Role = Role.Author }];
        epub.Type = book.BookType;
        epub.Year = book.Year;
        epub.Series = book.Series;
        epub.SeriesNumber = book.SeriesNumber;
        epub.Description = book.Description;
        
        epub.Save();
    }
    
    private void SaveState()
    {
        var state = new LibraryServiceState
        {
            Libraries = Libraries.ToList(),
            SelectedLibraryId = SelectedLibrary?.Id
        };

        var json = JsonSerializer.Serialize(state, _jsonOptions);
        File.WriteAllText(StateFilePath, json);
    }

    private void LoadState()
    {
        if (!File.Exists(StateFilePath))
            return;

        try
        {
            var json = File.ReadAllText(StateFilePath);
            var state = JsonSerializer.Deserialize<LibraryServiceState>(json);

            if (state == null)
                return;

            foreach (var library in state.Libraries)
            {
                _libraries.Add(new LibraryServiceCollection(library));
                LibraryAdded?.Invoke(this, library);
            }

            SelectedLibrary = Libraries.FirstOrDefault(lib => lib.Id == state.SelectedLibraryId);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public void Dispose()
    {
        foreach (var library in _libraries)
        {
            library.DatabaseService.Dispose();
        }
        
        GC.SuppressFinalize(this);
    }
}