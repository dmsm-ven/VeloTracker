using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using VeloTracker.ViewModels;
using Wpf.Ui.Controls;

namespace VeloTracker.Views;

public partial class MainWindow : FluentWindow
{
    private bool _rightClickOnRow;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += async (_, _) => await viewModel.LoadEventsCommand.ExecuteAsync(null);
    }

    // ПКМ по строке выделяет её, чтобы контекстное меню работало именно с ней.
    private void HistoryGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        var row = FindAncestor<System.Windows.Controls.DataGridRow>(e.OriginalSource as DependencyObject);
        _rightClickOnRow = row is not null;
        if (row is null) return;

        row.IsSelected = true;
        row.Focus();
    }

    // Меню показывается только над строкой (не над заголовком и не над пустым местом).
    private void HistoryGrid_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        var keyboardInvoked = e.CursorLeft < 0;
        var hasTarget = keyboardInvoked ? (sender as System.Windows.Controls.DataGrid)?.SelectedItem is not null : _rightClickOnRow;
        if (!hasTarget) e.Handled = true;
        _rightClickOnRow = false;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match) return match;
            current = current is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(current)
                : LogicalTreeHelper.GetParent(current);
        }
        return null;
    }
}
