using CommunityToolkit.Mvvm.ComponentModel;
using Ivy.Common.Models;

namespace Ivy.ViewModels;

public partial class MetadataSearchResultViewModel : ViewModelBase
{
    [ObservableProperty] private string _title;
    [ObservableProperty] private string? _author;
    [ObservableProperty] private string? _isbn;
    [ObservableProperty] private int? _publishedOn;
    [ObservableProperty] private int? _firstPublishedOn;
    [ObservableProperty] private string? _language;
    [ObservableProperty] private string? _description;
    [ObservableProperty] private string? _identifier;
    [ObservableProperty] private string? _identifierType;
    [ObservableProperty] private MetadataSearchResultCoverViewModel? _cover;
    
    public int? Year => PublishedOn ?? FirstPublishedOn;
    public string TruncatedTitle => Title.Length > 50 ? string.Concat(Title.AsSpan(0, 50), "...") : Title;
    public string? TruncatedAuthor => Author?.Length > 50 ? string.Concat(Author.AsSpan(0, 50), "...") : Author;
    
    public MetadataSearchResultViewModel(MetadataSearchResult model)
    {
        Title = model.Title;
        Author = model.Authors.FirstOrDefault();
        Isbn = model.Isbn;
        PublishedOn = model.PublishedOn;
        FirstPublishedOn = model.FirstPublishedOn;
        Language = model.Language;
        Description = model.Description;
        Identifier = model.Identifier;
        IdentifierType = model.IdentifierType;
        Cover = model.Cover is not null ? new MetadataSearchResultCoverViewModel(model.Cover) : null;
    }
}