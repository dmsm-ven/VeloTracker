using System.Windows;
using VeloTracker.ViewModels;
using Wpf.Ui.Controls;

namespace VeloTracker.Views;

public partial class MainWindow : FluentWindow
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadEventsCommand.ExecuteAsync(null);
    }
}
