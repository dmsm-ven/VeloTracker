using System.Windows;
using VeloTracker.ViewModels;
using Wpf.Ui.Controls;

namespace VeloTracker.Views;

public partial class StatisticsWindow : FluentWindow
{
    public StatisticsWindow(StatisticsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadCommand.ExecuteAsync(null);
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
