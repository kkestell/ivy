using CommunityToolkit.Mvvm.ComponentModel;
using Ivy.Common.Models;

namespace Ivy.ViewModels;

public partial class MetadataSearchResultCoverViewModel : ViewModelBase
{
    [ObservableProperty] private Uri? _thumbnail;
    [ObservableProperty] private Uri? _large;

    public MetadataSearchResultCoverViewModel(MetadataSearchResultCover model)
    {
        Thumbnail = model.Thumbnail;
        Large = model.Large;
    }
}