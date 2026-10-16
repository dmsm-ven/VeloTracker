using VeloTracker.ViewModels;
using Wpf.Ui.Controls;

namespace VeloTracker.Views;

public partial class SettingsWindow : FluentWindow
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadCommand.ExecuteAsync(null);
        viewModel.RequestClose += (_, _) => Close();
    }
}
