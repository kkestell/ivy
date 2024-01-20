using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Ivy.ViewModels;

public partial class ProgressViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _progressTitle = "Working";

    [ObservableProperty]
    private string _progressText = "Progress...";

    [ObservableProperty]
    private double _progressValue;
    
    [ObservableProperty]
    private bool _isCancelEnabled = true;
    
    public ICommand CancelCommand { get; }

    public event EventHandler? Cancelled;
    
    public ProgressViewModel()
    {
        CancelCommand = new RelayCommand(Cancel);
    }
    
    private void Cancel()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
        IsCancelEnabled = false;
    }
}