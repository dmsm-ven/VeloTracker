using System.Windows;
using VeloTracker.ViewModels;

namespace VeloTracker.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.RequestClose += (_, _) => Close();
    }
}
