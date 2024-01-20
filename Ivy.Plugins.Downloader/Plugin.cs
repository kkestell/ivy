using System.Diagnostics;
using Ivy.Plugins.Abstract;
using Ivy.Plugins.Downloader.ViewModels;
using Ivy.Plugins.Downloader.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Ivy.Plugins.Downloader;

public class Plugin : IPlugin
{
    private IPluginHost? _host;

    public void Initialize(IPluginHost host, IServiceCollection serviceCollection)
    {
        _host = host;

        serviceCollection.AddTransient<DownloaderViewModel>();
        
        host.AddMenuItem("Downloader", "Download Books...", ShowWindow);
    }

    private Task ShowWindow()
    {
        Debug.Assert(_host is not null);

        var window = new DownloaderWindow();
        window.Show();
        
        return Task.CompletedTask;
    }
}