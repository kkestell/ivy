using Ivy.Common.Models;

namespace Ivy.Plugins.Abstract;

public interface IPluginHost
{
    public IEnumerable<Book> SelectedBooks { get; }

    public List<IMetadataPlugin> MetadataPlugins { get; }

    void AddMenuItem(string pluginName, string menuItemName, Func<Task> action);
    
    void ImportBook(FileInfo epubFileInfo);

    void MessageBox(string message);
}