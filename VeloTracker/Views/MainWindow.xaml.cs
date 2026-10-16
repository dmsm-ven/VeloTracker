using System.Windows;
using VeloTracker.ViewModels;

namespace VeloTracker.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadEventsCommand.ExecuteAsync(null);
    }
}
