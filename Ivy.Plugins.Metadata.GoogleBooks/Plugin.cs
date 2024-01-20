using Ivy.Common;
using Ivy.Common.Models;
using Ivy.Plugins.Abstract;
using Microsoft.Extensions.DependencyInjection;

namespace Ivy.Plugins.Metadata.GoogleBooks
{
    public class Plugin : IMetadataPlugin
    {
        private readonly Client _client = new();
        private readonly CacheService<string, IEnumerable<MetadataSearchResult>> _cacheService = new();

        public string Name => "Google Books";

        public void Initialize(IPluginHost _, IServiceCollection __)
        {
        }

        public Task<IEnumerable<MetadataSearchResult>> Search(string isbn)
        {
            return _cacheService.GetOrAddAsync(isbn, () => _client.Search(isbn));
        }

        public Task<IEnumerable<MetadataSearchResult>> Search(string author, string title)
        {
            var key = $"{author}_{title}";
            return _cacheService.GetOrAddAsync(key, () => _client.Search(author, title));
        }
    }
}