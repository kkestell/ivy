using System.Diagnostics;
using Ivy.Common;
using Ivy.Common.Models;
using OpenLibraryNET;
using OpenLibraryNET.Data;

namespace Ivy.Plugins.Metadata.OpenLibrary;

public class Client
{
    private readonly OpenLibraryClient _client = new();

    public async Task<IEnumerable<MetadataSearchResult>> Search(string author, string title)
    {
        var parameters = new List<KeyValuePair<string, string>>();

        var works = new List<OLWorkData>();

        OLWorkData[]? results;

        results = await _client.Search.GetSearchResultsAsync($"title_suggest:{title} AND author:{author} AND language:eng", parameters.ToArray());
        if (results is not null)
            works.AddRange(results);

        return works.Select(x => WorkToSearchResult(x)).ToList();
    }

    public Task<IEnumerable<MetadataSearchResult>> Search(string isbn)
    {
        return Task.FromResult<IEnumerable<MetadataSearchResult>>(new List<MetadataSearchResult>());
    }

    private static OpenLibrarySearchResult WorkToSearchResult(OLWorkData work)
    {
        try
        {
            var authors = new List<string>();
            if (work.ExtensionData.ContainsKey("author_name"))
                authors = work.ExtensionData["author_name"].Values<string>().ToList();

            int? firstPublishedYear = null;
            if (work.ExtensionData.ContainsKey("first_publish_year"))
                firstPublishedYear = (int)work.ExtensionData["first_publish_year"];

            string? isbn = null;
            if (work.ExtensionData.ContainsKey("isbn"))
                isbn = work.ExtensionData["isbn"].Values<string>().FirstOrDefault(x => !string.IsNullOrEmpty(x) && IsbnValidator.IsValidIsbn(x));

            string? description = null;
            if (work.ExtensionData.ContainsKey("first_sentence"))
                description = work.ExtensionData["first_sentence"].Values<string>().FirstOrDefault();

            return new OpenLibrarySearchResult
            {
                Identifier = work.Key.Replace("/works/", ""),
                Isbn = isbn,
                Title = work.Title,
                Authors = authors,
                Description = description,
                FirstPublishedOn = firstPublishedYear,
                PublishedOn = null
            };
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            Debug.WriteLine(ex.StackTrace);
            throw;
        }
    }
}