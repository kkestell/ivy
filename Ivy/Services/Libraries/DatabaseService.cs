using Dapper;
using Ivy.Common.Models;
using Microsoft.Data.Sqlite;

namespace Ivy.Services.Libraries;

public class DatabaseService : IDisposable
{
    private readonly SqliteConnection _db;

    /// <summary>
    /// Initializes a new instance of the DatabaseService class, setting up the SQLite connection
    /// and creating the Books table if it doesn't already exist.
    /// </summary>
    /// <param name="libraryRoot">The directory where the database file will be stored.</param>
    public DatabaseService(FileSystemInfo libraryRoot)
    {
        var dbFilename = Path.Combine(libraryRoot.FullName, "Books.db");
        _db = new SqliteConnection($"Data Source={dbFilename}");
        _db.Open();

        _db.Execute("""
            CREATE TABLE IF NOT EXISTS Books (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Author TEXT NOT NULL,
                EpubPath TEXT NOT NULL,
                HasCover INTEGER NOT NULL DEFAULT 0,
                AddedOn DATETIME NOT NULL,
                BookType TEXT,
                Series TEXT,
                SeriesNumber INTEGER,
                Year INTEGER,
                Description TEXT
            );
        """);
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetAuthors()
    {
        return _db.Query<string>("SELECT DISTINCT Author FROM Books ORDER BY Author");
    }
    
    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetSeries()
    {
        return _db.Query<string>("SELECT DISTINCT Series FROM Books ORDER BY Series");
    }
    
    /// <summary>
    /// Retrieves all books from the database.
    /// </summary>
    /// <returns>An IEnumerable of Book representing all books in the database.</returns>
    public IEnumerable<Book> GetBooks()
    {
        return _db.Query<Book>("SELECT * FROM Books");
    }

    /// <summary>
    /// Imports a book into the database, setting its AddedOn date to the current UTC time.
    /// </summary>
    /// <param name="book">The book to be imported.</param>
    public void ImportBook(Book book)
    {
        book.AddedOn = DateTime.UtcNow;

        _db.Execute("""
            INSERT INTO Books (
                Title, 
                Author, 
                EpubPath, 
                HasCover, 
                AddedOn, 
                BookType, 
                Series, 
                SeriesNumber, 
                Year, 
                Description
            ) VALUES (
                @Title, 
                @Author, 
                @EpubPath, 
                @HasCover, 
                @AddedOn, 
                @BookType, 
                @Series, 
                @SeriesNumber, 
                @Year, 
                @Description
            )
        """, book);

        var id = _db.ExecuteScalar<int>("SELECT last_insert_rowid()");
        book.Id = id;
    }

    /// <summary>
    /// Updates the details of an existing book in the database.
    /// </summary>
    /// <param name="book">The book with updated information.</param>
    public void UpdateBook(Book book)
    {
        _db.Execute(
            "UPDATE Books SET Title = @Title, Author = @Author, BookType = @BookType, Series = @Series, SeriesNumber = @SeriesNumber, Year = @Year, Description = @Description, EpubPath = @EpubPath, HasCover = @HasCover WHERE Id = @Id",
            book);
    }

    /// <summary>
    /// Deletes a book from the database.
    /// </summary>
    /// <param name="book">The book to be deleted.</param>
    public void DeleteBook(Book book)
    {
        _db.Execute("DELETE FROM Books WHERE Id = @Id", book);
    }
    
    // /// <summary>
    // /// Checks if a book with the same title, author, series, and series number exists in the database.
    // /// </summary>
    // /// <param name="book">The book to check for existence.</param>
    // /// <returns>True if the book exists; otherwise, false.</returns>
    // public bool ContainsBook(Book book)
    // {
    //     var query = @"
    //         SELECT COUNT(*) 
    //         FROM Books 
    //         WHERE 
    //             Title = @Title 
    //             AND Author = @Author 
    //             AND Series = @Series 
    //             AND SeriesNumber = @SeriesNumber";
    //
    //     int count = _db.ExecuteScalar<int>(query, new 
    //     { 
    //         book.Title, 
    //         book.Author, 
    //         book.Series, 
    //         book.SeriesNumber 
    //     });
    //
    //     return count > 0;
    // }

    /// <summary>
    /// Disposes the database connection.
    /// </summary>
    public void Dispose()
    {
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}