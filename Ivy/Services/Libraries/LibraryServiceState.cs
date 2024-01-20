using Ivy.Common.Models;

namespace Ivy.Services.Libraries;

public class LibraryServiceState
{
    public List<Library> Libraries { get; init; } = [];
    public Guid? SelectedLibraryId { get; init; }
}