using System.Windows;
using SalesDistricts.Client.Services;

namespace SalesDistricts.Wpf;

public sealed class MessageBoxDialogService : IDialogService
{
    public bool Confirm(string message) =>
        MessageBox.Show(message, "Please confirm", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
}
