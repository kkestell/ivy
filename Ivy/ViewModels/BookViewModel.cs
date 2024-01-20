using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Ivy.Common.Models;

namespace Ivy.ViewModels;

public partial class BookViewModel(Book book) : ViewModelBase
{
    public BookViewModel() : this(new Book())
    {
    }

    public Book Model { get; } = book;

    public string Title
    {
        get => Model.Title;
        set => SetProperty(Model.Title, value, Model, (book, title) => book.Title = title);
    }

    public string Author
    {
        get => Model.Author;
        set => SetProperty(Model.Author, value, Model, (book, author) => book.Author = author);
    }

    public string? Series
    {
        get => Model.Series;
        set
        {
            SetProperty(Model.Series, value, Model, (book, series) => book.Series = series);
            OnPropertyChanged(nameof(DisplaySeries));
        }
    }

    public int? SeriesNumber
    {
        get => Model.SeriesNumber;
        set
        {
            SetProperty(Model.SeriesNumber, value, Model, (book, seriesNumber) => book.SeriesNumber = seriesNumber);
            OnPropertyChanged(nameof(DisplaySeries));
        }
    }

    public string? DisplaySeries
    {
        get
        {
            if (Series != null && SeriesNumber.HasValue)
            {
                return $"{Series} {SeriesNumber}";
            }

            return Series ?? string.Empty;
        }

        set
        {
            if (string.IsNullOrEmpty(value))
            {
                Series = null;
                SeriesNumber = null;
            }
            else
            {
                var match = SeriesRegex().Match(value);
                if (match.Success)
                {
                    Series = match.Groups["series"].Value.Trim();
                    SeriesNumber = int.Parse(match.Groups["number"].Value);
                }
                else
                {
                    Series = value.Trim();
                    SeriesNumber = null;
                }
            }
        }
    }

    public int? Year
    {
        get => Model.Year;
        set => SetProperty(Model.Year, value, Model, (book, year) => book.Year = year);
    }

    public string? BookType
    {
        get => Model.BookType;
        set => SetProperty(Model.BookType, value, Model, (book, bookType) => book.BookType = bookType);
    }
    
    public string? Description
    {
        get => Model.Description;
        set => SetProperty(Model.Description, value, Model, (book, description) => book.Description = description);
    }
    
    public string AddedOn => Model.AddedOn.ToLocalTime().ToString("MM/dd/yyyy hh:mm tt");

    // TODO
    public string Icon => string.Empty;

    [ObservableProperty] 
    private bool _coverChanged;

    private Bitmap? _cover;
    public Bitmap? Cover
    {
        get
        {
            if (_cover is not null)
                return _cover;

            if (Model.CoverPath is not null)
                _cover = new Bitmap(Model.CoverPath);
            
            return _cover;
        }
        set
        {
            SetProperty(ref _cover, value);

            if (value is null)
                return;
           
            const int targetWidth = 200;
            const int targetHeight = 300;
            
            var inputSize = new Size(value.PixelSize.Width, value.PixelSize.Height);

            // Calculate the scaling factor and new size
            var scalingFactor = Math.Max(targetWidth / inputSize.Width, targetHeight / inputSize.Height);
            var newSize = new Size(inputSize.Width * scalingFactor, inputSize.Height * scalingFactor);

            // Resize the image
            var resizedBitmap = value.CreateScaledBitmap(new PixelSize((int)newSize.Width, (int)newSize.Height));

            // Calculate the coordinates for cropping
            var startX = (resizedBitmap.PixelSize.Width - targetWidth) / 2;
            var startY = (resizedBitmap.PixelSize.Height - targetHeight) / 2;

            // Crop the image
            var croppedBitmap = new RenderTargetBitmap(new PixelSize(targetWidth, targetHeight));
            using (var ctx = croppedBitmap.CreateDrawingContext())
            {
                ctx.DrawImage(resizedBitmap, new Rect(startX, startY, targetWidth, targetHeight), new Rect(0, 0, targetWidth, targetHeight));
            }
            
            Thumbnail = croppedBitmap;
        }
    }

    private Bitmap? _thumbnail;
    public Bitmap? Thumbnail
    {
        get
        {
            if (_thumbnail is not null)
                return _thumbnail;
            
            return Model.ThumbnailPath is not null ? new Bitmap(Model.ThumbnailPath) : null;
        }
        
        set => SetProperty(ref _thumbnail, value);
    }

    [GeneratedRegex(@"^(?<series>.*)\s+(?<number>\d+)$")]
    private static partial Regex SeriesRegex();
}