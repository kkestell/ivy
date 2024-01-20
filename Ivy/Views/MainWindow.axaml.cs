using Avalonia.Controls;
using Avalonia.Interactivity;
using Ivy.ViewModels;

namespace Ivy.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Closed += OnClosed;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (View.DataContext is MainViewModel viewModel)
        {
            viewModel.UpdateWindowTitle();
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (View.DataContext is MainViewModel viewModel)
        {
            viewModel.Dispose();
        }
    }
}
