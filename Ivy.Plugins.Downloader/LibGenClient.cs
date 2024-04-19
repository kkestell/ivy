using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using AngleSharp.Html.Parser;
using Ivy.Common;
using Ivy.Plugins.Downloader.ViewModels;

namespace Ivy.Plugins.Downloader;

public class LibGenClient
{
    private readonly HttpClient _client;
    private readonly HtmlParser _parser;
    
    public LibGenClient()
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (HttpRequestMessage requestMessage, X509Certificate2 certificate, X509Chain chain, SslPolicyErrors sslErrors) => true;
        _client = new HttpClient(handler);
        
        _parser = new HtmlParser();
    }
    
    public async Task<List<SearchResultViewModel>> Search(string author, string title)
    {
        if (string.IsNullOrEmpty(author) && string.IsNullOrEmpty(title))
            return [];
        
        var query = Uri.EscapeDataString($"{author} {title}");
        var format = "epub";

        var currentPage = 1;
        int? numPages = null;
        var results = new List<SearchResultViewModel>();

        while (true)
        {
            if (results.Count >= 300)
                break;
            
            var url = $"https://libgen.is/fiction/?q={query}&language=English&format={format}&page={currentPage}";
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();

            var document = await _parser.ParseDocumentAsync(content);

            var rows = document.QuerySelectorAll(".catalog tbody tr");

            if (!numPages.HasValue)
            {
                var numResultsElement = document.QuerySelector("div.catalog_paginator > div");
                if (numResultsElement is null)
                    break;
                var numResultsText = numResultsElement.TextContent;
                numResultsText = new string(numResultsText.Where(char.IsDigit).ToArray());
                var numResults = int.Parse(numResultsText);
                numPages = (int)Math.Ceiling(numResults / (double)rows.Length);
            }

            foreach (var row in rows)
            {
                var cells = row.QuerySelectorAll("td");

                var authorListItems = cells[0].QuerySelectorAll("li");
                if (authorListItems.Length == 0) continue;

                var authors = new List<string>();
                foreach (var li in authorListItems)
                {
                    authors.Add(FixAuthor(li.TextContent));
                }

                var titleElement = cells[2].QuerySelector("a");
                var resultTitle = titleElement.TextContent;

                var isbn = cells[2].QuerySelector("p.catalog_identifier")?.TextContent;
                if (!string.IsNullOrEmpty(isbn))
                {
                    if (isbn.StartsWith("ISBN:"))
                    {
                        isbn = isbn.Replace("ISBN:", "").Trim();
                        
                        if (isbn.Contains(','))
                            isbn = isbn.Split(',')[0].Trim();
                        
                        if (!IsbnValidator.IsValidIsbn(isbn))
                            isbn = null;
                    }
                }

                var links = cells[5].QuerySelectorAll("a");
                if (links.Length == 0) continue;
                var urls = new List<string>();
                foreach (var link in links)
                {
                    var href = link.GetAttribute("href");
                    if (!string.IsNullOrEmpty(href))
                    {
                        urls.Add(href);
                    }
                }

                var fileInfo = cells[4].TextContent.Split('/');
                var fileType = fileInfo[0].Trim().ToLower();
                var size = fileInfo[1].Trim();

                var sizeInBytes = FileSizeToBytes(size);
                var sizeInMegabytes = (float)sizeInBytes / 1024 / 1024;
                var sizeStr = $"{sizeInMegabytes:F1} MB";

                int score;
                if (!string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(author))
                {
                    score = FuzzySharp.Fuzz.Ratio(title, resultTitle);
                    score += authors.Max(x => FuzzySharp.Fuzz.Ratio(author, x));

                    score /= 2;
                }
                else if (!string.IsNullOrEmpty(title))
                {
                    score = FuzzySharp.Fuzz.Ratio(title, resultTitle);
                }
                else
                {
                    score = authors.Max(x => FuzzySharp.Fuzz.Ratio(x, author));
                }

                var result = new SearchResultViewModel(authors, resultTitle, urls, isbn, fileType, sizeStr, score);
                results.Add(result);
            }

            currentPage++;

            if (currentPage > numPages) 
                break;
        }

        return results.ToList();
    }
    
    public async Task<string?> DownloadResult(SearchResultViewModel searchResultViewModel, Action<double>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        var url = searchResultViewModel.Urls[0];
        var response = await _client.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        var document = await _parser.ParseDocumentAsync(content);
        var downloadLink = document.QuerySelector("#download > h2 > a");
        if (downloadLink == null)
        {
            return null;
        }

        string downloadUrl = downloadLink.GetAttribute("href");
        string tempDir = Path.GetTempPath();
        string fileName = Path.Combine(tempDir, Path.GetRandomFileName() + $".{searchResultViewModel.FileType}");

        return await TryDownloadFile(downloadUrl, fileName, progressCallback, cancellationToken);
    }

    private async Task<string?> TryDownloadFile(string downloadUrl, string fileName, Action<double>? progressCallback = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
            var totalBytes = response.Content.Headers.ContentLength ?? 0;
            var bytesDownloaded = 0L;

            await using (var stream = await response.Content.ReadAsStreamAsync())
            await using (var fileStream = new FileStream(fileName, FileMode.Create))
            {
                var buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }
                    
                    fileStream.Write(buffer, 0, bytesRead);
                    bytesDownloaded += bytesRead;
                    var percent = Math.Round((double)bytesDownloaded / totalBytes * 100, 2);
                    progressCallback?.Invoke(percent);
                }
            }

            return fileName;
        }
        catch (Exception ex)
        {
            return null;
        }
    }
    
    private static string FixAuthor(string author)
    {
        if (author.Contains(','))
        {
            var parts = author.Split(',');
            if (parts.Length == 2)
            {
                return $"{parts[1].Trim()} {parts[0].Trim()}";
            }
        }
        return author;
    }

    private static int FileSizeToBytes(string size)
    {
        var parts = size.Split('\u00A0');
        if (parts.Length != 2) return 0;

        if (!double.TryParse(parts[0], out double numericSize)) return 0;

        switch (parts[1].ToLower())
        {
            case "kb":
                return (int)(numericSize * 1024);
            case "mb":
                return (int)(numericSize * 1024 * 1024);
            case "gb":
                return (int)(numericSize * 1024 * 1024 * 1024);
            default:
                return 0; // Unknown unit
        }
    }
}

public class DownloadProgressChangedEventArgs : EventArgs
{
    public double Progress { get; }

    public DownloadProgressChangedEventArgs(double progress)
    {
        Progress = progress;
    }
}
