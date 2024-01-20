using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ivy.ViewModels;

public partial class MessageBoxViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _message = "The quick brown fox jumps over the lazy dog. Lorem ipsum dolor sit amet. The quick brown fox jumps over the lazy dog. Lorem ipsum dolor sit amet.";

    public ICommand OkCommand => new RelayCommand(() => OkClicked?.Invoke(null, EventArgs.Empty));
    
    public event EventHandler? OkClicked;
}