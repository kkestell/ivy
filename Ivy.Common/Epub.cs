using System.IO.Compression;
using System.Xml.Linq;
using Avalonia.Media.Imaging;

namespace Ivy.Common;

public class Epub : IDisposable
{
    private readonly FileInfo _filePath;
    private readonly FileInfo _opfFilePath;
    private readonly DirectoryInfo _tempDir;

    public Epub(string filePath)
    {
        _filePath = new FileInfo(filePath);
        _tempDir = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
        _tempDir.Create();

        using (var epubStream = new FileStream(_filePath.FullName, FileMode.Open, FileAccess.Read))
        {
            using (var archive = new ZipArchive(epubStream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    var fullPath = Path.Combine(_tempDir.FullName, entry.FullName);
                    var directoryPath = Path.GetDirectoryName(fullPath)!;

                    if (!Directory.Exists(directoryPath))
                        Directory.CreateDirectory(directoryPath);

                    if (!string.IsNullOrEmpty(entry.Name))
                    {
                        entry.ExtractToFile(fullPath, true);
                    }
                }
            }
        }

        var containerFilePath = Path.Combine(_tempDir.FullName, "META-INF/container.xml");
        var containerFileInfo = new FileInfo(containerFilePath);

        using var containerFileReader = new StreamReader(containerFileInfo.OpenRead());
        var containerDoc = XDocument.Parse(containerFileReader.ReadToEnd());

        if (containerDoc.Root is null)
            throw new Exception("Root element not found in container file.");

        var rootFile = containerDoc.Root.Descendants().FirstOrDefault(x => x.Name.LocalName == "rootfile");

        var relativePath = rootFile?.Attribute("full-path")?.Value;

        if (string.IsNullOrEmpty(relativePath))
            throw new Exception("OPF path not found in container file.");

        _opfFilePath = new FileInfo(Path.Combine(_tempDir.FullName, relativePath) ??
                                    throw new Exception("OPF path not found in container file."));

        if (!_opfFilePath.Exists)
            throw new Exception("OPF file not found in EPUB.");

        using var opfFileReader = new StreamReader(_opfFilePath.OpenRead());
        var opfDoc = XDocument.Parse(opfFileReader.ReadToEnd());

        var metadataElement = opfDoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "metadata");
        if (metadataElement is null)
            throw new Exception("Metadata element not found in OPF file.");

        Title = GetMetadataValue(metadataElement, "title");
        Date = GetMetadataValue(metadataElement, "date");

        if (Date is not null)
        {
            if (DateTime.TryParse(Date, out var date))
                Year = date.Year;
            else if (int.TryParse(Date, out var year))
                Year = year;
        }

        Creators = ParseCreators(metadataElement);
        Contributors = ParseContributors(metadataElement);
        Identifiers = ParseIdentifiers(metadataElement);
        Series = GetMetadataMetaValue(metadataElement, "calibre:series");

        var seriesNumber = GetMetadataMetaValue(metadataElement, "calibre:series_index");
        if (int.TryParse(seriesNumber, out var seriesNumberInt))
            SeriesNumber = seriesNumberInt;

        Description = GetMetadataValue(metadataElement, "description");
        
        var type = GetMetadataValue(metadataElement, "type");
        if (Constants.BookTypes.Contains(type))
            Type = type;
        
        var manifestElement = opfDoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "manifest");
        if (manifestElement is null)
            throw new Exception("Manifest element not found in OPF file.");

        foreach (var child in manifestElement.Descendants().Where(x => x.Name.LocalName == "item"))
        {
            var id = child.Attribute("id")?.Value;
            var href = child.Attribute("href")?.Value;

            if (id is null || href is null)
                continue;

            if (!id.Contains("cover") || (!href.Contains("jpg") && !href.Contains("jpeg") && !href.Contains("png")))
                continue;

            var coverPath = Path.Combine(_opfFilePath.DirectoryName ?? throw new Exception("OPF directory not found."),
                href);
            var coverFileInfo = new FileInfo(coverPath);

            try
            {
                CoverFileInfo = coverFileInfo;

                if (coverFileInfo.Exists)
                {
                    using var coverStream = coverFileInfo.OpenRead();
                    CoverImage = new Bitmap(coverStream);
                }
            }
            catch (Exception e)
            {
                CoverFileInfo = null;
                CoverImage = null;
                
                Console.WriteLine(e);
            }
        }
    }

    public string? Title { get; set; }
    public string? Date { get; set; }
    public int? Year { get; set; }
    public List<Creator> Creators { get; set; }
    public List<Contributor> Contributors { get; set; }
    public List<Identifier> Identifiers { get; set; }
    public string? Series { get; set; }
    public int? SeriesNumber { get; set; }
    public string? Description { get; set; }
    public Bitmap? CoverImage { get; private set; }
    public FileInfo? CoverFileInfo { get; private set; }
    public string? Type { get; set; }

    public void UpdateCover(FileInfo newCoverFileInfo)
    {
        if (CoverFileInfo is null)
        {
            AddCover(newCoverFileInfo);
            return;
        }

        if (CoverFileInfo.Extension != newCoverFileInfo.Extension)
            return;

        newCoverFileInfo.CopyTo(CoverFileInfo.FullName, true);
    }

    private void AddCover(FileInfo coverFileInfo)
    {
        var opfDirectory = _opfFilePath.Directory;
        if (opfDirectory == null)
            throw new Exception("OPF directory not found.");

        var coverFileName = "cover" + coverFileInfo.Extension;
        var coverFilePath = Path.Combine(opfDirectory.FullName, coverFileName);
        coverFileInfo.CopyTo(coverFilePath, true);

        var xdoc = XDocument.Load(_opfFilePath.FullName);

        // Add to metadata
        var metadataElement = xdoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "metadata");
        if (metadataElement == null)
            throw new Exception("Metadata element not found in OPF file.");

        metadataElement.Add(new XElement("meta", new XAttribute("name", "cover"),
            new XAttribute("content", "cover-image")));

        // Add to manifest
        var manifestElement = xdoc.Descendants().FirstOrDefault(x => x.Name.LocalName == "manifest");
        if (manifestElement == null)
            throw new Exception("Manifest element not found in OPF file.");

        manifestElement.Add(new XElement("item",
            new XAttribute("href", coverFileName),
            new XAttribute("id", "cover-image"),
            new XAttribute("media-type", "image/jpeg")));

        xdoc.Save(_opfFilePath.FullName);

        // Update the cover image properties in the Epub class
        CoverFileInfo = new FileInfo(coverFilePath);
        using var coverStream = CoverFileInfo.OpenRead();
        CoverImage = new Bitmap(coverStream);
    }

    public void Save()
    {
        var xdoc = XDocument.Load(_opfFilePath.FullName);

        var versionAttribute = xdoc.Root?.Attribute("version");

        // FIXME: Nasty hack. Is this safe? Calibre refuses to load metadata if version is 3.0
        if (versionAttribute?.Value == "3.0")
        {
            versionAttribute.Value = "2.0";
        }

        var oldMetadataElements = xdoc.Descendants().Where(x => x.Name.LocalName == "metadata").ToList();

        // Retain the existing cover metadata if present
        var existingCoverMeta = oldMetadataElements.SelectMany(x => x.Elements())
            .FirstOrDefault(x => x.Name.LocalName == "meta" && x.Attribute("name")?.Value == "cover");

        foreach (var oldMetadata in oldMetadataElements)
        {
            oldMetadata.Remove();
        }

        // Define the specific namespaces
        XNamespace dc = "http://purl.org/dc/elements/1.1/";
        XNamespace opf = "http://www.idpf.org/2007/opf";

        // Create the new metadata element without a default namespace
        var newMetadata = new XElement("metadata",
            new XAttribute(XNamespace.Xmlns + "dc", dc),
            new XAttribute(XNamespace.Xmlns + "opf", opf));

        // Add the new metadata
        if (!string.IsNullOrEmpty(Title))
            newMetadata.Add(new XElement(dc + "title", Title));

        if (!string.IsNullOrEmpty(Series))
            newMetadata.Add(new XElement(dc + "meta", new XAttribute("name", "calibre:series"), Series));

        if (SeriesNumber.HasValue)
            newMetadata.Add(new XElement(dc + "meta", new XAttribute("name", "calibre:series_index"), SeriesNumber));

        if (!string.IsNullOrEmpty(Type))
            newMetadata.Add(new XElement(dc + "type", Type));

        if (!string.IsNullOrEmpty(Date))
            newMetadata.Add(new XElement(dc + "date", Date));

        if (!string.IsNullOrEmpty(Description))
            newMetadata.Add(new XElement(dc + "description", Description));

        if (Creators.Any())
        {
            foreach (var creator in Creators)
            {
                var creatorElement = new XElement(dc + "creator", creator.Name);

                if (!string.IsNullOrEmpty(creator.FileAs))
                    creatorElement.Add(new XAttribute("file-as", creator.FileAs));

                if (!string.IsNullOrEmpty(creator.Role))
                    creatorElement.Add(new XAttribute("role", creator.Role));

                newMetadata.Add(creatorElement);
            }
        }

        newMetadata.Add(new XElement(dc + "language", "en"));

        // Add the retained cover metadata
        if (existingCoverMeta != null)
        {
            newMetadata.Add(existingCoverMeta);
        }

        // Add the new metadata to the document
        xdoc.Root?.Add(newMetadata);

        // Save the document        
        xdoc.Save(_opfFilePath.FullName);

        // Move original epub to backup
        var backupPath = Path.Combine(_filePath.DirectoryName ?? string.Empty,
            Path.GetFileNameWithoutExtension(_filePath.Name) + "_backup.epub");
        File.Move(_filePath.FullName, backupPath);

        // Create new epub
        ZipFile.CreateFromDirectory(_tempDir.FullName, _filePath.FullName);

        // Clean up
        File.Delete(backupPath);
    }

    public void Dispose()
    {
        _tempDir.Delete(true);
    }

    public static string AuthorNameToFileAs(string authorName)
    {
        var names = authorName.Split(' ');
        var lastName = names.Last();
        var firstNames = names.Take(names.Length - 1);
        return $"{lastName}, {string.Join(" ", firstNames)}";
    }

    private string? GetMetadataValue(XElement metadataElement, string localName, string? attributeName = null)
    {
        var element = metadataElement.Descendants().FirstOrDefault(x => x.Name.LocalName == localName);

        if (element is null)
            return null;

        if (attributeName is null)
            return element.Value.Trim();

        return element.Attribute(attributeName)?.Value.Trim();
    }

    private string? GetMetadataMetaValue(XElement metadataElement, string name)
    {
        var element = metadataElement.Descendants()
            .FirstOrDefault(x => x.Name.LocalName == "meta" && x.Attribute("name")?.Value == name);

        if (element is null)
            return null;

        return element.Value.Trim();
    }

    private List<Creator> ParseCreators(XElement metadataElement)
    {
        var creators = new List<Creator>();

        foreach (var child in metadataElement.Descendants().Where(x => x.Name.LocalName == "creator"))
        {
            creators.Add(new Creator
            {
                Name = child.Value.Trim(),
                FileAs = child.Attributes().FirstOrDefault(x => x.Name.LocalName == "file-as")?.Value.Trim() ??
                         string.Empty,
                Role = child.Attributes().FirstOrDefault(x => x.Name.LocalName == "role")?.Value.Trim() ?? string.Empty
            });
        }

        return creators;
    }

    private List<Contributor> ParseContributors(XElement metadataElement)
    {
        var contributors = new List<Contributor>();

        foreach (var child in metadataElement.Descendants().Where(x => x.Name.LocalName == "contributor"))
        {
            contributors.Add(new Contributor
            {
                Name = child.Value.Trim(),
                FileAs = child.Attributes().FirstOrDefault(x => x.Name.LocalName == "file-as")?.Value.Trim() ??
                         string.Empty,
                Role = child.Attributes().FirstOrDefault(x => x.Name.LocalName == "role")?.Value.Trim() ?? string.Empty
            });
        }

        return contributors;
    }

    private List<Identifier> ParseIdentifiers(XElement metadataSoup)
    {
        var identifiers = new List<Identifier>();

        foreach (var child in metadataSoup.Descendants().Where(x => x.Name.LocalName == "identifier"))
        {
            identifiers.Add(new Identifier
            {
                Value = child.Value.Trim(),
                Scheme = child.Attributes().FirstOrDefault(x => x.Name.LocalName == "scheme")?.Value.Trim() ??
                         string.Empty,
                Type = child.Attributes().FirstOrDefault(x => x.Name.LocalName == "type")?.Value.Trim() ?? string.Empty
            });
        }

        return identifiers;
    }
}

public class Contributor
{
    public string Name { get; set; }
    public string FileAs { get; set; }
    public string? Role { get; set; }
}

public class Creator
{
    public string Name { get; set; }
    public string FileAs { get; set; }
    public string? Role { get; set; }
}

public class Identifier
{
    public string Value { get; set; }
    public string Scheme { get; set; }
    public string Type { get; set; }
}