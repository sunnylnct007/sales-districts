namespace SalesDistricts.Client.Services;

// Lets ViewModels ask the user something without knowing about windows; tests supply a fake.
public interface IDialogService
{
    bool Confirm(string message);
}
