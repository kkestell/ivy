using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using Ivy.Common.Models;
using Ivy.Plugins.Abstract;
using Ivy.Services;
using Ivy.Services.Libraries;

namespace Ivy;

public class PluginHost(WindowService windowService, LibraryService libraryService) : IPluginHost
{
    public IEnumerable<Book> SelectedBooks => libraryService.SelectedBooks;
    
    public List<IMetadataPlugin> MetadataPlugins { get; } = [];

    public void AddMenuItem(string pluginName, string menuItemName, Func<Task> action)
    {
        var mainWindow = windowService.MainWindow;
        
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var pluginMenu = mainWindow.View.FindMenuItem(pluginName);
            if (pluginMenu is null)
            {
                pluginMenu = new MenuItem { Header = pluginName };
                mainWindow.View.AddPluginMenu(pluginMenu);
            }
            pluginMenu.Items.Add(new MenuItem
            {
                Header = menuItemName,
                Command = new AsyncRelayCommand(action)
            });
        });    
    }

    public void ImportBook(FileInfo epubFileInfo)
    {
        libraryService.ImportBook(epubFileInfo);
    }
}