using Ivy.Common.Models;

namespace Ivy.Services.Libraries;

internal class LibraryServiceCollection
{
    public Library Library { get; }
    public FileService FileService { get; }
    public DatabaseService DatabaseService { get; }

    public LibraryServiceCollection(Library library)
    {
        Library = library;
        FileService = new FileService(new DirectoryInfo(library.Path));
        DatabaseService = new DatabaseService(new DirectoryInfo(library.Path));
    }
}