using System.Windows;
using SalesDistricts.Client.ViewModels;

namespace SalesDistricts.Wpf;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
        Loaded += async (_, _) => await viewModel.LoadCommand.ExecuteAsync(null);
    }
}
