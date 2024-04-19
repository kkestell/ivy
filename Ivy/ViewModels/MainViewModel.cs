using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading;
using Ivy.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Input;
using System.Xml;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ivy.Common.Models;
using Ivy.Services.Libraries;
using Ivy.Views;

namespace Ivy.ViewModels;

public enum ViewMode
{
    Icons,
    Details
}

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly LibraryService _libraryService;
    private readonly WindowService _windowService;

    /// <summary>
    /// 
    /// </summary>
    public LibraryViewModel? SelectedLibrary
    {
        get => _libraryService.SelectedLibrary is null ? null : new LibraryViewModel(_libraryService.SelectedLibrary);
        set => _libraryService.SelectedLibrary = value?.Model;
    }
    
    /// <summary>
    /// 
    /// </summary>
    public List<LibraryViewModel> Libraries => _libraryService.Libraries.Select(x => new LibraryViewModel(x)).ToList();

    /// <summary>
    /// 
    /// </summary>
    public ObservableCollection<BookViewModel> Books { get; set; } = [];
    
    /// <summary>
    /// 
    /// </summary>
    public ObservableCollection<BookViewModel> SelectedBooks { get; } = [];
    
    /// <summary>
    /// 
    /// </summary>
    public IEnumerable<string> Authors => _libraryService.Authors;

    /// <summary>
    /// 
    /// </summary>
    public IEnumerable<string> Series => _libraryService.Series;

    /// <summary>
    /// 
    /// </summary>
    [ObservableProperty] 
    private ViewMode _viewMode = ViewMode.Details;

    public ICommand NewLibraryCommand => new AsyncRelayCommand(NewLibrary);
    public ICommand SelectLibraryCommand => new RelayCommand<LibraryViewModel>(library => SelectedLibrary = library);
    public ICommand ImportBookCommand => new AsyncRelayCommand(ImportBook);
    public ICommand ImportBooksFromDirectoryCommand => new AsyncRelayCommand(ImportBooksFromDirectory);
    public ICommand ShowBookCommand => new AsyncRelayCommand(ShowBook);
    public ICommand OpenBookCommand => new AsyncRelayCommand(OpenBook);
    public ICommand EditBooksCommand => new AsyncRelayCommand(EditBooks);
    public ICommand SyncMetadataCommand => new AsyncRelayCommand(SyncMetadata);
    public ICommand DeleteBooksCommand => new AsyncRelayCommand(DeleteBooks);
    public ICommand SaveBookCommand => new AsyncRelayCommand<BookViewModel>(SaveBook);
    public ICommand ChangeViewModeCommand => new RelayCommand<ViewMode>(ChangeViewMode);
    
    public MainViewModel(WindowService windowService, LibraryService libraryService)
    {
        _windowService = windowService;
        _libraryService = libraryService;

        _libraryService.LibraryAdded += OnLibraryAdded;
        _libraryService.LibrarySelected += OnLibrarySelected;
        _libraryService.BookImported += OnBookImported;
        _libraryService.BookDeleted += OnBookDeleted;
        
        SelectedBooks.CollectionChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(SelectedBooks));
        };
    }

    public MainViewModel() : this(
        DesignTimeServices.Services.GetRequiredService<WindowService>(),
        DesignTimeServices.Services.GetRequiredService<LibraryService>())
    {
    }

    public void SelectBooks(List<BookViewModel> books)
    {
        SelectedBooks.Clear();
        foreach (var book in books)
        {
            SelectedBooks.Add(book);
        }
        
        _libraryService.SelectedBooks = SelectedBooks.Select(x => x.Model).ToList();
    }

    public void UpdateWindowTitle()
    {
        if (_libraryService.SelectedLibrary is null)
            return;
        
        _windowService.SetMainWindowTitle($"Ivy - {_libraryService.SelectedLibrary.Name}");
    }
    
    public void Refresh()
    {
        if (_libraryService.SelectedLibrary is null)
            return;
        
        Books = new ObservableCollection<BookViewModel>(
            _libraryService.SelectedLibrary.Books
                .OrderBy(book => book.Author)
                .ThenByDescending(book => book.Series != null)
                .ThenBy(book => book.Series)
                .ThenBy(book => book.SeriesNumber)
                .ThenBy(book => book.Title)
                .Select(book => new BookViewModel(book))
                .ToList()
        );
        
        OnPropertyChanged(nameof(Books));
    }
    
    private async Task SaveBook(BookViewModel? book)
    {
        Debug.Assert(book is not null);

        try
        {
            if (book.CoverChanged)
            {
                _libraryService.UpdateCover(book.Model, book.Cover!);
                book.CoverChanged = false;
            }

            _libraryService.UpdateBook(book.Model);

            OnPropertyChanged(nameof(Authors));
        }
        catch (Exception ex)
        {
            await _windowService.ShowMessageBox("Error saving book.", ex);
        }
    }
    
    private void ChangeViewMode(ViewMode viewMode)
    {
        ViewMode = viewMode;
        OnPropertyChanged(nameof(ViewMode));
    }

    public void Dispose()
    {
        _libraryService.LibraryAdded -= OnLibraryAdded;
        _libraryService.LibrarySelected -= OnLibrarySelected;
        _libraryService.BookImported -= OnBookImported;
        _libraryService.BookDeleted -= OnBookDeleted;

        _libraryService.Dispose();
        
        GC.SuppressFinalize(this);
    }
    
    private void OnLibraryAdded(object? sender, Library? library)
    {
        OnPropertyChanged(nameof(Libraries));
    }

    private void OnLibrarySelected(object? sender, Library? library)
    {
        OnPropertyChanged(nameof(SelectedLibrary));
        Refresh();
    }

    private void OnBookImported(object? sender, Book book)
    {
        Books.Add(new BookViewModel(book));
    }
    
    private void OnBookDeleted(object? sender, Book book)
    {
        var existingBook = Books.FirstOrDefault(x => x.Model.Id == book.Id);
        
        if (existingBook is not null)
             Books.Remove(existingBook);
    }

    private async Task NewLibrary()
    {
        var newLibraryWindow = new NewLibraryWindow();
        var newLibraryViewModel = (NewLibraryViewModel)newLibraryWindow.View.DataContext!;

        newLibraryViewModel.SaveRequested += (_, _) =>
        {
            newLibraryWindow.Close();
            _libraryService.AddLibrary(newLibraryViewModel.Library.Model);
        };

        await _windowService.ShowDialog(newLibraryWindow);
    }

    private async Task ImportBook()
    {
        Debug.Assert(SelectedLibrary is not null);

        var epubFile = await _windowService.ChooseOpenFile(
            "Choose an EPUB file",
            [new FilePickerFileType("EPUB Files") { Patterns = ["*.epub"] }]);

        if (epubFile is null)
            return;
        
        _libraryService.ImportBook(new FileInfo(epubFile.Path.LocalPath));
    }

    private async Task ImportBooksFromDirectory()
    {
        Debug.Assert(SelectedLibrary is not null);

        var importDirectory = await _windowService.ChooseDirectory("Choose a directory to import books from");

        if (importDirectory is null)
            return;

        await _windowService.ShowProgressWindow("Importing Books", DoImportBooksFromDirectory, importDirectory);
    }

    private Task DoImportBooksFromDirectory(IProgress<ProgressUpdate> progressCallback, DirectoryInfo importDirectory, CancellationToken stoppingToken)
    {
        return Task.Run(() =>
        {
            var files = importDirectory.EnumerateFiles("*.epub", SearchOption.AllDirectories).ToList();
            
            var total = files.Count;
            var progress = 0;
            var percentComplete = 0.0;
            
            for (var i = 0; i < total; i++)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                var file = files[i];

                progressCallback.Report(new ProgressUpdate
                {
                    PercentComplete = percentComplete,
                    Message = $"Importing book {i + 1} of {total}"
                });

                try
                {
                    _libraryService.ImportBook(file);
                }
                catch (InvalidDataException e)
                {
                    Debug.WriteLine($"Epub file {file.FullName} is invalid: {e.Message}");
                }
                catch (XmlException e)
                {
                    Debug.WriteLine($"Epub file {file.FullName} contains invalid XML: {e.Message}");
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"Failed to import book {file.FullName}: {e.Message}");
                }

                progress++;
                percentComplete = (double)progress / total * 100;
            }
            
            progressCallback.Report(new ProgressUpdate
            {
                PercentComplete = 100,
                Message = "Finished importing books"
            });
            
        }, stoppingToken);
    }

    private async Task ShowBook()
    {
        var firstSelectedBook = SelectedBooks.FirstOrDefault();
        
        if (firstSelectedBook is null)
            return;

        var filePath = firstSelectedBook.Model.EpubPath;
        
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Process.Start("explorer.exe", $"/select,\"{filePath}\"");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process.Start("open", $"-R \"{filePath}\"");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            Process.Start("xdg-open", filePath);
        }
        else
        {
            await _windowService.ShowMessageBox("Operating system not supported.");
        }
    }

    private async Task OpenBook()
    {
        var firstSelectedBook = SelectedBooks.FirstOrDefault();
        
        if (firstSelectedBook is null)
            return;

        var filePath = firstSelectedBook.Model.EpubPath;
        
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        var calibreCommand = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? @"C:\Program Files\Calibre2\ebook-viewer.exe"
            : "ebook-viewer";

        try
        {
            Process.Start(calibreCommand, $"\"{filePath}\"");
        }
        catch (Exception ex)
        {
            await _windowService.ShowMessageBox("Failed to open file in Calibre.", ex);
        }
    }
    
    private async Task EditBooks()
    {
        Debug.Assert(SelectedLibrary is not null);
        Debug.Assert(SelectedBooks.Count >= 1);

        var editBookWindow = new EditBookWindow();
        var editBookViewModel = (EditBookViewModel)editBookWindow.View.DataContext!;

        foreach (var book in SelectedBooks)
        {
            editBookViewModel.Books.Add(book);
        }

        await _windowService.ShowDialog(editBookWindow);
        
        foreach (var book in editBookViewModel.Books)
        {
            await SaveBook(book);
        }
    }
    
    private async Task SyncMetadata()
    {
        Debug.Assert(SelectedLibrary is not null);
        Debug.Assert(SelectedBooks.Count >= 1);

        var books = SelectedBooks.ToList();
        
        await _windowService.ShowProgressWindow("Syncing Metadata", DoSyncMetadata, books);
    }
    
    private static Task DoSyncMetadata(IProgress<ProgressUpdate> progressCallback, List<BookViewModel> books, CancellationToken stoppingToken)
    {
        return Task.Run(() =>
        {
            var total = books.Count;
            var progress = 0;
            var percentComplete = 0.0;
            
            for (var i = 0; i < total; i++)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                var book = books[i];

                progressCallback.Report(new ProgressUpdate
                {
                    PercentComplete = percentComplete,
                    Message = $"Syncing metadata for book {i + 1} of {total}"
                });

                try
                {
                    LibraryService.SyncMetadata(book.Model);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }

                progress++;
                percentComplete = (double)progress / total * 100;
            }
            
            progressCallback.Report(new ProgressUpdate
            {
                PercentComplete = 100,
                Message = "Finished syncing metadata"
            });
            
        }, stoppingToken);
    }

    private async Task DeleteBooks()
    {
        Debug.Assert(SelectedLibrary is not null);
        Debug.Assert(SelectedBooks.Count >= 1);

        var books = SelectedBooks.ToList();
        
        await _windowService.ShowProgressWindow("Deleting Books", DoDeleteBooks, books);
    }
    
    private Task DoDeleteBooks(IProgress<ProgressUpdate> progressCallback, List<BookViewModel> books, CancellationToken stoppingToken)
    {
        return Task.Run(() =>
        {
            var total = books.Count;
            var progress = 0;
            var percentComplete = 0.0;
            
            for (var i = 0; i < total; i++)
            {
                if (stoppingToken.IsCancellationRequested)
                    break;

                var book = books[i];

                progressCallback.Report(new ProgressUpdate
                {
                    PercentComplete = percentComplete,
                    Message = $"Deleting book {i + 1} of {total}"
                });

                _libraryService.DeleteBook(book.Model);

                progress++;
                percentComplete = (double)progress / total * 100;
            }
            
            progressCallback.Report(new ProgressUpdate
            {
                PercentComplete = 100,
                Message = "Finished deleting books"
            });
            
        }, stoppingToken);
    }
}