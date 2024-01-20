using CommunityToolkit.Mvvm.ComponentModel;

namespace Ivy.ViewModels;

public class ViewModelBase : ObservableValidator
{
    public bool IsValid
    {
        get
        {
            ValidateAllProperties();
            return !HasErrors;
        }
    }
}
