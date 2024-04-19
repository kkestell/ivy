using System.Text.Json.Serialization;

namespace Ivy.Common.Models;

public class Library
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Name { get; set; } = string.Empty;
    
    public string Path { get; set; } = string.Empty;
    
    [JsonIgnore]
    public List<Book> Books { get; set; } = new();
    
    public bool ContainsBook(Book book)
    {
        return Books.Any(x =>
            x.Title == book.Title && x.Author == book.Author && x.Series == book.Series &&
            x.SeriesNumber == book.SeriesNumber);
    }
}