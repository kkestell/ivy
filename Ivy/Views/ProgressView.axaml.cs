using Avalonia.Controls;
using Ivy.Common;
using Ivy.ViewModels;

namespace Ivy.Views;

public partial class ProgressView : UserControl
{
    public ProgressView()
    {
        InitializeComponent();
        DataContext = this.CreateInstance<ProgressViewModel>();
    }
}