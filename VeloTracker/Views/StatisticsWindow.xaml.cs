using VeloTracker.ViewModels;
using Wpf.Ui.Controls;

namespace VeloTracker.Views;

public partial class StatisticsWindow : FluentWindow
{
    public StatisticsWindow(StatisticsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
