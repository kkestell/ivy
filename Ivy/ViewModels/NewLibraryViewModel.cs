using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ivy.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Ivy.ViewModels;

public partial class NewLibraryViewModel : ViewModelBase
{
    private readonly WindowService _windowService;
    
    [ObservableProperty]
    private LibraryViewModel _library = new();
    
    public event EventHandler? SaveRequested;
    
    public ICommand CreateLibraryCommand => new RelayCommand(() =>
    {
        if (Library.IsValid)
            SaveRequested?.Invoke(this, EventArgs.Empty);
    });

    public ICommand BrowseCommand => new AsyncRelayCommand(Browse);
    
    public NewLibraryViewModel(WindowService windowService)
    {
        _windowService = windowService;
    }
    
    public NewLibraryViewModel() : this(DesignTimeServices.Services.GetRequiredService<WindowService>())
    {
    }

    private async Task Browse()
    {
        var path = await _windowService.ChooseDirectory("Choose a directory to store your library in.");
        
        if (path is null)
            return;
        
        Library.Path = path.FullName;
    }
}