using Ivy.Common.Models;

namespace Ivy.Plugins.Abstract;

public interface IMetadataPlugin : IPlugin
{
    public string Name { get; }
    public Task<IEnumerable<MetadataSearchResult>> Search(string author, string title);
}