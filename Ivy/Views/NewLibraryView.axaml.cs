using Avalonia.Controls;
using Ivy.Common;
using Ivy.ViewModels;

namespace Ivy.Views;

public partial class NewLibraryView : UserControl
{
    public NewLibraryView()
    {
        InitializeComponent();
        DataContext = this.CreateInstance<NewLibraryViewModel>();
    }
}