using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Ivy.Common;
using Ivy.ViewModels;

namespace Ivy.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();

        var viewModel = this.CreateInstance<MainViewModel>();
        
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.SelectedLibrary))
            {
                UpdateMenuItems();
            }
        };
        
        DataContext = viewModel;
        
        UpdateMenuItems();
        viewModel.Refresh();
    }
    
    public MenuItem? FindMenuItem(string header)
    {
        foreach (var item in PluginsMenuItem.Items)
        {
            if (item is MenuItem menuItem && menuItem.Header?.ToString() == header)
            {
                return menuItem;
            }
        }
        return null;
    }

    public void AddPluginMenu(MenuItem menuItem)
    {
        PluginsMenuItem.Items.Add(menuItem);
    }
    
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var viewModel = (MainViewModel)DataContext!;

        var selectedItems = sender switch
        {
            DataGrid dataGrid => dataGrid.SelectedItems,
            ListBox listBox => listBox.SelectedItems,
            _ => throw new InvalidOperationException("Unknown sender type")
        };

        if (selectedItems is null)
            return;

        viewModel.SelectBooks(selectedItems.Cast<BookViewModel>().ToList());
    }
    
    private void UpdateMenuItems()
    {
        var viewModel = (MainViewModel)DataContext!;
        
        PluginsMenuItem.IsEnabled = viewModel.SelectedLibrary is not null;

        LibrariesMenuItem.IsEnabled = viewModel.Libraries.Count != 0;
        LibrariesMenuItem.Items.Clear();
     
        if (!LibrariesMenuItem.IsEnabled)
            return;
     
        foreach (var library in viewModel.Libraries)
        {
            var libraryMenuItem = new MenuItem
            {
                Tag = library.Id,
                Header = library.Name,
                Icon = CreateIcon(string.Empty),
                Command = viewModel.SelectLibraryCommand,
                CommandParameter = library
            };
     
            LibrariesMenuItem.Items.Add(libraryMenuItem);
        }
     
        foreach (MenuItem? item in LibrariesMenuItem.Items)
        {
            Debug.Assert(item is not null);
            var icon = (TextBlock)item.Icon!;
            icon.Text = (Guid)item.Tag! == viewModel.SelectedLibrary?.Id
                ? Icons.RadioButtonChecked
                : Icons.RadioButtonUnchecked;
        }
    }
    
    private static TextBlock CreateIcon(string iconText) => new()
    {
        Text = iconText,
        FontFamily = (FontFamily)Application.Current!.Resources["MaterialSymbolsFont"]!,
        FontSize = 16,
        Width = 16,
        Height = 16
    };

    private void RowEditEnded(object? sender, DataGridRowEditEndedEventArgs e)
    {
        var viewModel = (MainViewModel)DataContext!;
        viewModel.SaveBookCommand.Execute(e.Row.DataContext);
    }
}
