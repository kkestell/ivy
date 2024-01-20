using System.Net;
using System.Text.Json;
using Ivy.Common;
using Ivy.Common.Models;

namespace Ivy.Plugins.Metadata.GoogleBooks;

public class Client
{
        private readonly HttpClient _httpClient;

    public Client()
    {
        _httpClient = new HttpClient();
    }

    public async Task<IEnumerable<MetadataSearchResult>> Search(string isbn)
    {
        var results = new List<GoogleSearchResult>();

        var requestUri = $"https://www.googleapis.com/books/v1/volumes?q=isbn:{isbn}";
        var response = await _httpClient.GetAsync(requestUri);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            var totalItemsElement = root.GetProperty("totalItems");

            if (totalItemsElement.GetInt32() == 0)
                return results;

            var itemsElement = root.GetProperty("items");

            var items = itemsElement.EnumerateArray();

            foreach (var item in items)
            {
                var result = await ParseItemToResult(item);

                if (result == null)
                    continue;
                
                results.Add(result);
            }
        }

        return results;
    }

    public async Task<IEnumerable<MetadataSearchResult>> Search(string author, string title)
    {
        var results = new List<GoogleSearchResult>();

        var query = $"{author} {title}";
        var encodedQuery = WebUtility.UrlEncode(query);
        var requestUri = $"https://www.googleapis.com/books/v1/volumes?q={encodedQuery}";
        
        var cnt = 1;
        while (true)
        {
            var response = await _httpClient.GetAsync(requestUri);

            if (response.IsSuccessStatusCode)
            {
                results.AddRange(await ResponseToResults(response));
                break;
            }

            Thread.Sleep(cnt * 1000);
        
            cnt++;
            if (cnt > 5)
                break;
        }
        
        // query = $"{title}";
        // encodedQuery = WebUtility.UrlEncode(query);
        // requestUri = $"https://www.googleapis.com/books/v1/volumes?q={encodedQuery}";
        // response = await _httpClient.GetAsync(requestUri);
        //
        // if (response.IsSuccessStatusCode)
        // {
        //     results.AddRange(await ResponseToResults(response));
        // }

        return results;
    }

    private async Task<IEnumerable<GoogleSearchResult>> ResponseToResults(HttpResponseMessage response)
    {
        var content = response.Content.ReadAsStringAsync().Result;
        using var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;
        var totalItemsElement = root.GetProperty("totalItems");
        if (totalItemsElement.GetInt32() == 0)
            return new List<GoogleSearchResult>();
        var itemsElement = root.GetProperty("items");

        var results = new List<GoogleSearchResult>();
        foreach (var item in itemsElement.EnumerateArray())
        {
            var result = await ParseItemToResult(item);

            if (result is null)
                continue;
            
            results.Add(result);
        }

        return results;
    }

    private async Task<GoogleSearchResult?> ParseItemToResult(JsonElement item)
    {
        var identifier = string.Empty;
        if (item.TryGetProperty("id", out var idElement))
            identifier = idElement.GetString();

        if (identifier is null)
            return null;

        if (!item.TryGetProperty("volumeInfo", out var volumeInfo))
            return null;

        var title = string.Empty;
        if (volumeInfo.TryGetProperty("title", out var titleElement))
            title = titleElement.GetString() ?? string.Empty;

        string? isbn = null;
        if (volumeInfo.TryGetProperty("industryIdentifiers", out var industryIdentifiersElement))
        {
            foreach (var industryIdentifierElement in industryIdentifiersElement.EnumerateArray())
            {
                if (industryIdentifierElement.TryGetProperty("type", out var typeElement))
                {
                    var type = typeElement.GetString();

                    if (type == "ISBN_13")
                    {
                        if (industryIdentifierElement.TryGetProperty("identifier", out var identifierElement))
                        {
                            var candidate = identifierElement.GetString();
                            if (IsbnValidator.IsValidIsbn(candidate))
                                isbn = candidate;

                            break;
                        }
                    }
                    else if (type == "ISBN_10")
                    {
                        if (industryIdentifierElement.TryGetProperty("identifier", out var identifierElement))
                        {
                            var candidate = identifierElement.GetString();
                            if (IsbnValidator.IsValidIsbn(candidate))
                                isbn = candidate;

                            break;
                        }
                    }
                }
            }
        }

        var authors = new List<string>();
        if (volumeInfo.TryGetProperty("authors", out var authorsElement))
        {
            foreach (var authorElement in authorsElement.EnumerateArray())
            {
                var authorName = authorElement.GetString();

                if (authorName is not null)
                    authors.Add(authorName);
            }
        }

        var firstPublishedYear = ParsePublishedDate(volumeInfo);
        
        var result = new GoogleSearchResult
        {
            Title = title,
            Authors = authors,
            Identifier = identifier,
            Isbn = isbn,
            PublishedOn = firstPublishedYear
        };

        string? language = null;
        if (volumeInfo.TryGetProperty("language", out var languageElement))
            language = languageElement.GetString();
        result.Language = language;

        string? textSnippet = null;
        if (volumeInfo.TryGetProperty("description", out var descriptionElement))
            textSnippet = descriptionElement.GetString();

        if (!string.IsNullOrEmpty(textSnippet))
            result.Description = WebUtility.HtmlDecode(textSnippet);
        
        // result.Cover = await FetchCover(identifier);

        return result;
    }

    private int? ParsePublishedDate(JsonElement volumeInfo)
    {
        if (!volumeInfo.TryGetProperty("publishedDate", out var publishedDateElement))
            return null;
        
        var dateString = publishedDateElement.GetString();

        if (dateString is null)
            return null;

        if (DateTime.TryParse(dateString, out var date))
        {
            return date.Year;
        }

        if (dateString.Length == 4 && int.TryParse(dateString, out var year))
        {
            return year;
        }

        return null;
    }
    
    // private async Task<MetadataSearchResultCover?> FetchCover(string volumeId)
    // {
    //     var requestUri = $"https://www.googleapis.com/books/v1/volumes/{volumeId}?fields=id,volumeInfo(title,imageLinks)";
    //     var response = await _httpClient.GetAsync(requestUri);
    //     if (response.IsSuccessStatusCode)
    //     {
    //         string content = await response.Content.ReadAsStringAsync();
    //         using var doc = JsonDocument.Parse(content);
    //         if (doc.RootElement.TryGetProperty("volumeInfo", out var volumeInfo) &&
    //             volumeInfo.TryGetProperty("imageLinks", out var imageLinks))
    //         {
    //             var cover = new MetadataSearchResultCover();
    //             var imageSizes = new string[] { "extraLarge", "large", "medium", "small", "thumbnail", "smallThumbnail" };
    //
    //             foreach (var size in imageSizes)
    //             {
    //                 if (imageLinks.TryGetProperty(size, out var imageElement))
    //                 {
    //                     var urlString = imageElement.GetString();
    //                     if (!string.IsNullOrEmpty(urlString))
    //                     {
    //                         var currentUri = new Uri(urlString);
    //                         switch (size)
    //                         {
    //                             case "extraLarge":
    //                                 cover.ExtraLarge = currentUri;
    //                                 break;
    //                             case "large":
    //                                 cover.Large = currentUri;
    //                                 break;
    //                             case "medium":
    //                                 cover.Medium = currentUri;
    //                                 break;
    //                             case "small":
    //                                 cover.Small = currentUri;
    //                                 break;
    //                             case "thumbnail":
    //                                 cover.Thumbnail = currentUri;
    //                                 break;
    //                             case "smallThumbnail":
    //                                 cover.SmallThumbnail = currentUri;
    //                                 break;
    //                         }
    //                     }
    //                 }
    //             }
    //
    //             return cover;
    //         }
    //     }
    //
    //     return null;
    // }
}