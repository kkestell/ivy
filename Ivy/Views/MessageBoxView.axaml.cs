using Avalonia.Controls;
using Ivy.Common;
using Ivy.ViewModels;

namespace Ivy.Views;

public partial class MessageBoxView : UserControl
{
    public MessageBoxView()
    {
        InitializeComponent();
        DataContext = this.CreateInstance<MessageBoxViewModel>();
    }
}