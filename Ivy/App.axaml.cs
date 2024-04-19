using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Ivy.Plugins.Abstract;
using Ivy.Services;
using Ivy.Services.Libraries;
using Ivy.ViewModels;
using Ivy.Views;

using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Ivy;

public class App : Application
{
    private IServiceCollection _serviceCollection = new ServiceCollection();
    private IServiceProvider _serviceProvider;
    
    public App()
    {
        _serviceProvider = _serviceCollection.BuildServiceProvider();
    }
    
    public override void Initialize()
    {
        // ConfigureServices();
        //
        // Resources.Add(typeof(IServiceProvider), _serviceProvider);
            
        AvaloniaXamlLoader.Load(this);
    }
    
    private static string LogFilePath()
    {
        string folderPath;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Library", "Logs");
        }
        else
        {
            var xdgCacheHome = Environment.GetEnvironmentVariable("XDG_CACHE_HOME");
            folderPath = !string.IsNullOrEmpty(xdgCacheHome) ? xdgCacheHome : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".cache");
        }

        return Path.Combine(folderPath, "ivy", "ivy.log");
    }

    public static void ConfigureServices(IServiceCollection serviceCollection)
    {
        Log.Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.File(LogFilePath(), rollingInterval: RollingInterval.Day)
            .CreateLogger();
        
        Log.Information("Starting Ivy");
        
        serviceCollection.AddLogging(loggingBuilder =>
            loggingBuilder.AddSerilog(dispose: true));
        
        serviceCollection.AddTransient<MainViewModel>(provider =>
        {
            var windowService = provider.GetRequiredService<WindowService>();
            var libraryService = provider.GetRequiredService<LibraryService>();
            return new MainViewModel(windowService, libraryService);
        });
        
        serviceCollection.AddTransient<NewLibraryViewModel>(provider =>
        {
            var windowService = provider.GetRequiredService<WindowService>();
            return new NewLibraryViewModel(windowService);
        });
        
        serviceCollection.AddTransient<ProgressViewModel>();
        
        serviceCollection.AddSingleton<MainWindow>();
        
        serviceCollection.AddSingleton<LibraryService>();
        
        serviceCollection.AddSingleton<WindowService>(provider =>
        {
            var mainWindowProvider = new Lazy<MainWindow>(provider.GetRequiredService<MainWindow>);
            return new WindowService(mainWindowProvider);
        });
        
        serviceCollection.AddSingleton<IPluginHost, PluginHost>(provider =>
        {
            var windowService = provider.GetRequiredService<WindowService>();
            var libraryService = provider.GetRequiredService<LibraryService>();
            return new PluginHost(windowService, libraryService);
        });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _serviceCollection = new ServiceCollection();
        
        ConfigureServices(_serviceCollection);
        
        _serviceProvider = _serviceCollection.BuildServiceProvider();

        Resources.Add(typeof(IServiceProvider), _serviceProvider);
        
        // Line below is needed to remove Avalonia data validation.
        // Without this line you will get duplicate validations from both Avalonia and CT
        BindingPlugins.DataValidators.RemoveAt(0);

        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                break;
            case ISingleViewApplicationLifetime singleViewPlatform:
                singleViewPlatform.MainView = new MainView();
                break;
        }

        LoadPlugins();
        
        base.OnFrameworkInitializationCompleted();
    }
    
    private void LoadPlugins()
    {
        var pluginHost = _serviceProvider.GetRequiredService<IPluginHost>();
        
        foreach (var file in Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll"))
        {
            var assembly = Assembly.LoadFrom(file);
            foreach (var type in assembly.GetTypes())
            {
                try
                {
                    if (!typeof(IPlugin).IsAssignableFrom(type) || type.IsInterface)
                        continue;

                    if (Activator.CreateInstance(type) is not IPlugin plugin)
                        continue;
                    
                    Log.Information($"Loading plugin {type.FullName}");

                    plugin.Initialize(pluginHost, _serviceCollection);
                    
                    if (typeof(IMetadataPlugin).IsAssignableFrom(type) && !type.IsInterface)
                        pluginHost.MetadataPlugins.Add((IMetadataPlugin)plugin);    
                }
                catch (Exception e)
                {
                    Log.Error(e, $"Error loading plugin {type.FullName}");
                }
            }
        }
    }
}
