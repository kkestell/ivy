using CommunityToolkit.Mvvm.ComponentModel;
using Ivy.Common;

namespace Ivy.Plugins.Downloader.ViewModels;

public partial class DownloadJobViewModel : ObservableObject
{
    [ObservableProperty] private Guid _id;
    [ObservableProperty] private string? _author;
    [ObservableProperty] private string? _title;
    [ObservableProperty] private string? _searchAuthor;
    [ObservableProperty] private string? _searchTitle;
    [ObservableProperty] private double _progress;
    
    public bool IsCancelable => Status == DownloadStatus.Downloading;
    
    public bool IsRemovable => Status != DownloadStatus.Downloading;
    
    private DownloadStatus _status;
    public DownloadStatus Status
    {
        get => _status;
        set 
        {
            SetProperty(ref _status, value);
            OnPropertyChanged(nameof(Icon));
            OnPropertyChanged(nameof(IsCancelable));
            OnPropertyChanged(nameof(IsRemovable));
        }
    }
    
    public string Icon
    {
        get
        {
            switch (Status)
            {
                case DownloadStatus.Pending:
                    return Icons.Pending;
                case DownloadStatus.Downloading:
                    return Icons.Downloading;
                case DownloadStatus.Done:
                    return Icons.DownloadDone;
                case DownloadStatus.Error:
                    return Icons.Error;
                case DownloadStatus.Canceled:
                    return Icons.Cancel;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}