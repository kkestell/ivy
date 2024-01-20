using System.Threading;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Ivy.ViewModels;
using Ivy.Views;

namespace Ivy.Services;

public class WindowService
{
    private readonly Lazy<MainWindow> _mainWindow;
        
    public WindowService(Lazy<MainWindow> mainWindow)
    {
        _mainWindow = mainWindow;
    }
    
    public MainWindow MainWindow => _mainWindow.Value;
    
    public void SetMainWindowTitle(string title)
    {
        _mainWindow.Value.Title = title;
    }

    public async Task ShowProgressWindow<T>(string title, Func<IProgress<ProgressUpdate>, T, CancellationToken, Task> action, T argument) where T : notnull
    {
        var cts = new CancellationTokenSource();
        
        var wnd = new ProgressWindow
        {
            Title = title,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Width = 400,
            SizeToContent = SizeToContent.Height,
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.NoChrome,
            CanResize = false,
            ExtendClientAreaToDecorationsHint = true,
            ShowInTaskbar = false,
            ExtendClientAreaTitleBarHeightHint = -1,
            SystemDecorations = SystemDecorations.BorderOnly
        };

        var progressViewModel = (ProgressViewModel)wnd.View.DataContext!;

        progressViewModel.Cancelled += (_, _) =>
        {
            cts.Cancel();
            wnd.Close();
        };
        
        _ = wnd.ShowDialog(_mainWindow.Value);

        await action.Invoke(
            new Progress<ProgressUpdate>(progressUpdate =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    progressViewModel.ProgressText = progressUpdate.Message;
                    progressViewModel.ProgressValue = progressUpdate.PercentComplete;
                });
            }), 
            argument,
            cts.Token);
        
        wnd.Close();
    }

    public Task ShowMessageBox(string message)
    {
        var window = new MessageBoxWindow();
        var viewModel = (MessageBoxViewModel)window.View.DataContext!;
        viewModel.OkClicked += (_, _) => window.Close();
        viewModel.Message = message;
        return window.ShowDialog(_mainWindow.Value);
    }
    
    public Task ShowMessageBox(string message, Exception ex) => ShowMessageBox($"{message}\n\n{ex.Message}\n\n{ex.StackTrace}");
    
    public Task ShowDialog(Window window) => window.ShowDialog(_mainWindow.Value);
        
    public async Task<IStorageFile?> ChooseOpenFile(string title, FilePickerFileType[] fileTypes)
    {
        var opts = new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter = fileTypes
        };

        var selectedFiles = await _mainWindow.Value.StorageProvider.OpenFilePickerAsync(opts);
        
        return selectedFiles.Count == 0 ? null : selectedFiles[0];
    }
        
    // public Task<IStorageFile?> ChooseSaveFile(string title, FilePickerFileType[] fileTypes)
    // {
    //     var opts = new FilePickerSaveOptions
    //     {
    //         Title = title,
    //         FileTypeChoices = fileTypes
    //     };
    //
    //     return _mainWindow.Value.StorageProvider.SaveFilePickerAsync(opts);
    // }
        
    public async Task<DirectoryInfo?> ChooseDirectory(string title)
    {
        var opts = new FolderPickerOpenOptions
        {
            AllowMultiple = false,
            Title = title
        };
        
        var directoryPaths = await _mainWindow.Value.StorageProvider.OpenFolderPickerAsync(opts);
        
        return directoryPaths.Count == 0 ? null : new DirectoryInfo(directoryPaths[0].Path.LocalPath);
    }
}