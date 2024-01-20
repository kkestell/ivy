using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using Ivy.Common.Models;

namespace Ivy.ViewModels;

public class LibraryViewModel : ViewModelBase
{
    public LibraryViewModel(Library model)
    {
        Model = model;
    }

    public LibraryViewModel() : this(new Library())
    {
    }
    
    public Library Model { get; }
    
    public Guid Id => Model.Id;

    [Required]
    [MinLength(1)]
    public string Name
    {
        get => Model.Name;
        set
        {
            SetProperty(Model.Name, value, Model, (library, name) => library.Name = name);
            ValidateProperty(Model.Name);
        }
    }

    [Required]
    [CustomValidation(typeof(LibraryViewModel), nameof(ValidatePath))]
    public string Path
    {
        get => Model.Path;
        set
        {
            SetProperty(Model.Path, value, Model, (library, path) => library.Path = path);
            ValidateProperty(Model.Path);
        }
    }

    public ObservableCollection<BookViewModel> Books
    {
        get => new(Model.Books.Select(book => new BookViewModel(book)));
        set => SetProperty(Model.Books, value.Select(book => book.Model).ToList(), Model, (library, books) => library.Books = books);
    }
    
    public void AddBook(BookViewModel book)
    {
        Model.Books.Add(book.Model);
        OnPropertyChanged(nameof(Books));
    }
    
    public static ValidationResult? ValidatePath(string path, ValidationContext context)
    {
        try
        {
            var directoryInfo = new DirectoryInfo(path);

            if (!directoryInfo.Exists)
            {
                return new ValidationResult("The path does not exist");
            }
        }
        catch
        {
            return new ValidationResult("The path is not valid");
        }

        return ValidationResult.Success;
    }
}
