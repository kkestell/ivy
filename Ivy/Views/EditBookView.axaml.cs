using Avalonia.Controls;
using Avalonia.Input;
using Ivy.Common;
using Ivy.ViewModels;

namespace Ivy.Views;

public partial class EditBookView : UserControl
{
    public EditBookView()
    {
        InitializeComponent();
        DataContext = this.CreateInstance<EditBookViewModel>();
        
        AddHandler(DragDrop.DropEvent, Drop);
        AddHandler(DragDrop.DragOverEvent, DragOver);
    }
    
    private static void DragOver(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.FileNames))
            return;
        
        e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }
    
    private void Drop(object? sender, DragEventArgs e)
    {
        if (!e.Data.Contains(DataFormats.Files)) 
            return;
        
        var firstFile = e.Data.GetFiles()!.ToList()[0];
            
        var viewModel = (EditBookViewModel)DataContext!;

        Debug.Assert(viewModel.Book is not null);
        
        viewModel.Book.Cover = new Avalonia.Media.Imaging.Bitmap(firstFile.Path.LocalPath);
        viewModel.Book.CoverChanged = true;
            
        e.Handled = true;
    }

    private void SearchResult_OnDoubleTapped(object? sender, TappedEventArgs e)
    {
        var viewModel = (EditBookViewModel)DataContext!;
        var selectedResult = (MetadataSearchResultViewModel)SearchResultGrid.SelectedItem;

        Debug.Assert(viewModel.Book is not null);
        Debug.Assert(selectedResult is not null);
        
        viewModel.Book.Title = selectedResult.Title;
        
        if (selectedResult.Author is not null)
            viewModel.Book.Author = selectedResult.Author;
    
        if (selectedResult.Year is not null)
            viewModel.Book.Year = selectedResult.Year;
        
        if (selectedResult.Description is not null)
            viewModel.Book.Description = selectedResult.Description;
    }
}