using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ivy.Common;
using Ivy.Plugins.Downloader.ViewModels;

namespace Ivy.Plugins.Downloader.Views;

public partial class DownloaderView : UserControl
{
    public DownloaderView()
    {
        InitializeComponent();
        DataContext = this.CreateInstance<DownloaderViewModel>();
    }
    
    private void OnSearchResultsSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var vm = (DownloaderViewModel)DataContext!;
        vm.SelectedSearchResults.Clear();
        foreach (SearchResultViewModel result in ((DataGrid)sender).SelectedItems)
        {
            vm.SelectedSearchResults.Add(result);
        }
    }

    private void QueryTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is DownloaderViewModel viewModel)
        {
            viewModel.SearchCommand.Execute(null);
        }
    }

    private void OnDownloadJobsSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var vm = (DownloaderViewModel)DataContext!;
        vm.SelectedDownloadJobs.Clear();
        foreach (DownloadJobViewModel result in ((ListBox)sender!).SelectedItems!)
        {
            vm.SelectedDownloadJobs.Add(result);
        }
    }

    private void OnDownloadsButtonClick(object? sender, RoutedEventArgs e)
    {
        var ctl = sender as Control;

        if (ctl is not null)
        {
            FlyoutBase.ShowAttachedFlyout(ctl);
        }
    }
}