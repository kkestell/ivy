using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Ivy.Common;
using Ivy.Plugins.Abstract;
using Serilog;

namespace Ivy.Plugins.Downloader.ViewModels;

public partial class DownloaderViewModel : ObservableObject
{
    private readonly IPluginHost _host;
    private readonly ConcurrentStack<SearchResultViewModel> _downloadStack;
    private readonly CancellationTokenSource _downloadQueueCancellationTokenSource;
    private CancellationTokenSource _downloadCancellationTokenSource = new();
    private Task _downloadTask;

    [ObservableProperty] private string? _author = string.Empty;
    [ObservableProperty] private string? _title = string.Empty;

    [ObservableProperty] private string _downloadsButtonIcon = Icons.Circle;
    [ObservableProperty] private bool _isDownloadsButtonEnabled = false;

    public ObservableCollection<SearchResultViewModel> SearchResults { get; } = [];
    
    public ObservableCollection<SearchResultViewModel> SelectedSearchResults { get; } = [];
    
    public ObservableCollection<DownloadJobViewModel> SelectedDownloadJobs { get; } = [];
    
    public ObservableCollection<DownloadJobViewModel> DownloadJobs { get; } = [];
    
    public ICommand SearchCommand { get; }
    public ICommand RepeatSearchCommand { get; }
    public ICommand CancelDownloadCommand { get; }
    public ICommand RemoveDownloadCommand { get; }
    public ICommand DownloadCommand { get; }

    public DownloaderViewModel(IPluginHost host)
    {
        _host = host;
        _downloadStack = new ConcurrentStack<SearchResultViewModel>();
        _downloadQueueCancellationTokenSource = new CancellationTokenSource();
        _downloadTask = Task.Run(() => ProcessDownloadQueueAsync(_downloadQueueCancellationTokenSource.Token));

        SearchCommand = new AsyncRelayCommand(Search);
        DownloadCommand = new RelayCommand(EnqueueDownload);
        RepeatSearchCommand = new AsyncRelayCommand(RepeatSearch);
        CancelDownloadCommand = new AsyncRelayCommand(CancelDownload);
        RemoveDownloadCommand = new AsyncRelayCommand(RemoveDownload);

        if (Design.IsDesignMode)
        {
            DownloadJobs.Add(new DownloadJobViewModel { Author = "Arthur C. Clarke", Title = "Against the Fall of Night XXXXXX XXXXXXXXXXXX", Progress = 50 });
            DownloadJobs.Add(new DownloadJobViewModel { Author = "Arthur C. Clarke", Title = "The Light of Other Days", Progress = 27 });
            DownloadJobs.Add(new DownloadJobViewModel { Author = "Arthur C. Clarkeeeeeeeeeeeeeeeeeeeeeeeeee", Title = "Richter 10", Progress = 90 });
            DownloadJobs.Add(new DownloadJobViewModel { Author = "Arthur C. Clarke", Title = "Against the Fall of Night XXXXXX XXXXXXXXXXXX", Progress = 50 });
            DownloadJobs.Add(new DownloadJobViewModel { Author = "Arthur C. Clarke", Title = "The Light of Other Days", Progress = 27 });
            DownloadJobs.Add(new DownloadJobViewModel { Author = "Arthur C. Clarkeeeeeeeeeeeeeeeeeeeeeeeeee", Title = "Richter 10", Progress = 90 });
            DownloadJobs.Add(new DownloadJobViewModel { Author = "Arthur C. Clarke", Title = "Against the Fall of Night XXXXXX XXXXXXXXXXXX", Progress = 50 });
            DownloadJobs.Add(new DownloadJobViewModel { Author = "Arthur C. Clarke", Title = "The Light of Other Days", Progress = 27 });
            DownloadJobs.Add(new DownloadJobViewModel { Author = "Arthur C. Clarkeeeeeeeeeeeeeeeeeeeeeeeeee", Title = "Richter 10", Progress = 90 });
        }
    }

    private async Task Search()
    {
        var client = new LibGenClient();
        var results = await client.Search(Author, Title);
        var sortedResults = results.OrderByDescending(result => result.Score).ToList();
        
        SearchResults.Clear();

        foreach (var result in sortedResults)
            SearchResults.Add(result);
    }
    
    private Task RepeatSearch()
    {
        var firstSelectedDownloadJob = SelectedDownloadJobs.FirstOrDefault();
        
        if (firstSelectedDownloadJob is null)
            return Task.CompletedTask;
        
        Author = firstSelectedDownloadJob.SearchAuthor;
        Title = firstSelectedDownloadJob.SearchTitle;
        
        return Search();
    }
   
    private Task CancelDownload()
    {
        _downloadCancellationTokenSource.Cancel();
        
        return Task.CompletedTask;
    }
    
    private Task RemoveDownload()
    {
        var firstSelectedDownloadJob = SelectedDownloadJobs.FirstOrDefault();
        
        if (firstSelectedDownloadJob is null)
            return Task.CompletedTask;
        
        var downloadJob = DownloadJobs.FirstOrDefault(job => job.Id == firstSelectedDownloadJob.Id);

        if (downloadJob is null)
            return Task.CompletedTask;
        
        DownloadJobs.Remove(downloadJob);
        
        return Task.CompletedTask;
    }
    
    private void EnqueueDownload()
    {
        foreach (var searchResult in SelectedSearchResults)
        {
            _downloadStack.Push(searchResult);
            var downloadJob = new DownloadJobViewModel
            {
                Id = searchResult.Id,
                SearchAuthor = Author,
                SearchTitle = Title,
                Author = searchResult.Authors.FirstOrDefault() ?? "Unknown Author",
                Title = searchResult.Title
            };
            DownloadJobs.Insert(0, downloadJob);
        }
    }

    private async Task ProcessDownloadQueueAsync(CancellationToken cancellationToken)
    {
        var client = new LibGenClient();

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_downloadStack.TryPop(out var searchResult))
            {
                DownloadsButtonIcon = Icons.Downloading;
                IsDownloadsButtonEnabled = true;

                var downloadJob = DownloadJobs.FirstOrDefault(job => job.Id == searchResult.Id);

                Debug.Assert(downloadJob is not null);

                try
                {
                    downloadJob.Status = DownloadStatus.Downloading;
                    
                    _downloadCancellationTokenSource = new CancellationTokenSource();

                    var localPath = await client.DownloadResult(searchResult, progress =>
                    {
                        Dispatcher.UIThread.Invoke(() => downloadJob.Progress = progress);
                    }, _downloadCancellationTokenSource.Token);

                    if (localPath is null)
                    {
                        if (_downloadCancellationTokenSource.IsCancellationRequested)
                        {
                            downloadJob.Status = DownloadStatus.Canceled;
                        }
                        else
                        {
                            downloadJob.Status = DownloadStatus.Error;
                        }
                        
                        continue;
                    }

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try
                        {
                            _host.ImportBook(new FileInfo(localPath));
                        }
                        catch(Exception ex)
                        {
                            _host.MessageBox(ex.Message);
                        }
                    });

                    downloadJob.Status = DownloadStatus.Done;
                }
                catch (Exception e)
                {
                    downloadJob.Status = DownloadStatus.Error;
                    Log.Error(e, "Error downloading book");
                }
            }
            else
            {
                if (DownloadJobs.Any() && DownloadJobs.All(x => x.Status == DownloadStatus.Done))
                {
                    DownloadsButtonIcon = Icons.DownloadDone;
                }

                await Task.Delay(1000, cancellationToken);
            }
        }
    }

    public void Dispose()
    {
        _downloadQueueCancellationTokenSource.Cancel();
        _downloadTask.Wait();
    }
}