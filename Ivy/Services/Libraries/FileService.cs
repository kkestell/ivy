using Avalonia.Media.Imaging;
using Ivy.Common.Models;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Ivy.Services.Libraries;

public class FileService
{
    private readonly DirectoryInfo _libraryRoot;

    /// <summary>
    /// Initializes a new instance of the FileService class for managing files in a specified library root directory.
    /// </summary>
    /// <param name="libraryRoot">The root directory for the library where files will be managed.</param>
    /// <exception cref="IOException">Thrown if an I/O error occurs while creating the library root directory.</exception>
    public FileService(DirectoryInfo libraryRoot)
    {
        _libraryRoot = libraryRoot;

        if (!_libraryRoot.Exists)
        {
            _libraryRoot.Create();
        }
    }

    /// <summary>
    /// Copies the book and its cover (if provided) to the library directory.
    /// </summary>
    /// <param name="book">The book to import.</param>
    /// <param name="epubFileInfo">File information for the book's EPUB file.</param>
    /// <param name="coverFileInfo">Optional file information for the book's cover image.</param>
    /// <exception cref="FormatException">Thrown when the cover file cannot be converted to JPG format.</exception>
    public void ImportBook(Book book, FileInfo epubFileInfo, FileInfo? coverFileInfo = null)
    {
        var bookDirectory = BookDirectory(book);
        var bookFile = BookFile(book);

        if (!bookDirectory.Exists)
        {
            bookDirectory.Create();
        }

        epubFileInfo.CopyTo(bookFile.FullName, overwrite: true);
        book.EpubPath = bookFile.FullName;

        try
        {
            if (coverFileInfo is null) 
                return;
            
            var coverPath = Path.Combine(bookFile.Directory!.FullName, "cover.jpg");

            var convertedCover = ConvertToJpg(coverFileInfo);

            if (convertedCover is null)
            {
                throw new FormatException($"Error adding cover. Could not convert to JPG: {coverFileInfo.FullName}");
            }

            convertedCover.CopyTo(coverPath, overwrite: true);

            GenerateThumbnail(book);

            book.HasCover = true;
        }
        catch (Exception e)
        {
            book.HasCover = false;
            
            Log.Error(e, "Error adding cover");
        }
    }

    private static void GenerateThumbnail(Book book)
    {
        var coverPath = Path.Combine(Path.GetDirectoryName(book.EpubPath)!, "cover.jpg");
        using var image = Image.Load(coverPath);

        const int targetWidth = 200;
        const int targetHeight = 300;

        // Resize the image to fill the target dimensions while preserving the aspect ratio
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(targetWidth, targetHeight),
            Mode = ResizeMode.Crop
        }));

        // Calculate the coordinates to crop the image to the target size
        var startX = Math.Max(0, (image.Width - targetWidth) / 2);
        var startY = Math.Max(0, (image.Height - targetHeight) / 2);

        // Crop the image to the target size
        image.Mutate(x => x.Crop(new Rectangle(startX, startY, targetWidth, targetHeight)));

        var thumbPath = Path.Combine(Path.GetDirectoryName(book.EpubPath)!, "thumb.jpg");
        image.Save(thumbPath);
    }

    /// <summary>
    /// Updates the filename of the book and moves the book's files to a new location if necessary.
    /// </summary>
    /// <param name="book">The book to update.</param>
    public void UpdateBook(Book book)
    {
        var oldEpubFile = new FileInfo(book.EpubPath);

        var newEpubFile = BookFile(book);

        if (oldEpubFile.FullName == newEpubFile.FullName)
            return;

        book.EpubPath = newEpubFile.FullName;

        var oldBookDirectory = oldEpubFile.Directory!;
        var oldAuthorDirectory = oldBookDirectory.Parent!;

        var newBookDirectory = newEpubFile.Directory!;

        if (!newBookDirectory.Exists)
            newBookDirectory.Create();

        oldEpubFile.MoveTo(newEpubFile.FullName);

        foreach (var file in oldBookDirectory.GetFiles())
        {
            file.MoveTo(Path.Combine(newBookDirectory.FullName, file.Name));
        }

        if (oldBookDirectory.GetFiles().Length == 0)
            oldBookDirectory.Delete();

        var remainingBookDirectories = oldAuthorDirectory.GetDirectories();
        if (remainingBookDirectories.Length == 0)
            oldAuthorDirectory.Delete();
    }

    /// <summary>
    /// Deletes a book's files and directories.
    /// </summary>
    /// <param name="book">The book to delete.</param>
    /// <exception cref="IOException">Thrown if the book's directory does not exist or an error occurs during deletion.</exception>
    public void DeleteBook(Book book)
    {
        var bookDirectory = BookDirectory(book);

        if (bookDirectory.Exists)
            bookDirectory.Delete(recursive: true);

        var authorDirectory = bookDirectory.Parent!;

        if (!authorDirectory.Exists)
            return;

        var remainingBooks = authorDirectory.GetDirectories();
        if (remainingBooks.Length == 0)
            authorDirectory.Delete();
    }

    public void UpdateCover(Book book, Bitmap coverBitmap)
    {
        var tempFileName = Path.GetTempFileName() + ".jpg";
        coverBitmap.Save(tempFileName);

        var tempFileInfo = new FileInfo(tempFileName);

        UpdateCover(book, tempFileInfo);
    }

    private void UpdateCover(Book book, FileInfo coverFileInfo)
    {
        var bookDirectory = BookDirectory(book);
        var coverFile = CoverFile(book);

        if (!bookDirectory.Exists)
        {
            throw new Exception($"Error adding cover. Directory not found: {bookDirectory.FullName}");
        }

        var convertedCover = ConvertToJpg(coverFileInfo);

        if (convertedCover is null)
        {
            throw new Exception($"Error adding cover. Could not convert to JPG: {coverFileInfo.FullName}");
        }

        convertedCover.CopyTo(coverFile.FullName, overwrite: true);

        GenerateThumbnail(book);

        book.HasCover = true;
    }

    private static FileInfo? ConvertToJpg(FileInfo coverFileInfo)
    {
        try
        {
            var format = Image.DetectFormat(coverFileInfo.FullName);

            if (format.Name == "JPEG")
                return coverFileInfo;

            using var image = Image.Load(coverFileInfo.FullName);

            var tempFileName = Path.GetTempFileName() + ".jpg";
            image.SaveAsJpeg(tempFileName);

            return new FileInfo(tempFileName);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private DirectoryInfo BookDirectory(Book book)
    {
        var author = SanitizeForPath(book.Author);

        if (string.IsNullOrEmpty(author))
            author = "Unknown Author";

        var title = SanitizeForPath(book.Title);

        if (string.IsNullOrEmpty(title))
            title = "Unknown Title";

        DirectoryInfo bookDirectory;

        if (!string.IsNullOrEmpty(book.Series) && book.SeriesNumber.HasValue)
        {
            var series = SanitizeForPath(book.Series);
            bookDirectory = new DirectoryInfo(Path.Combine(_libraryRoot.FullName, author,
                $"{series} {book.SeriesNumber} - {title}"));
        }
        else
        {
            bookDirectory = new DirectoryInfo(Path.Combine(_libraryRoot.FullName, author, title));
        }

        return bookDirectory;
    }

    private FileInfo BookFile(Book book)
    {
        var bookDirectory = BookDirectory(book);

        var author = SanitizeForPath(book.Author);

        if (string.IsNullOrEmpty(author))
            author = "Unknown Author";

        var title = SanitizeForPath(book.Title);

        if (string.IsNullOrEmpty(title))
            title = "Unknown Title";

        string bookFileName;

        if (!string.IsNullOrEmpty(book.Series) && book.SeriesNumber.HasValue)
        {
            var series = SanitizeForPath(book.Series);
            bookFileName = $"{author} - {series} {book.SeriesNumber} - {title}.epub";
        }
        else
        {
            bookFileName = $"{author} - {title}.epub";
        }

        return new FileInfo(Path.Combine(bookDirectory.FullName, bookFileName));
    }

    private FileInfo CoverFile(Book book)
    {
        var bookDirectory = BookDirectory(book);
        return new FileInfo(Path.Combine(bookDirectory.FullName, "cover.jpg"));
    }

    private static string SanitizeForPath(string input)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized =
            invalidChars.Aggregate(input, (current, invalidChar) => current.Replace(invalidChar.ToString(), ""));

        if (sanitized.Length > 32)
            sanitized = sanitized[..32];

        sanitized = sanitized.Trim('.');
        sanitized = sanitized.Trim();

        return sanitized;
    }
}