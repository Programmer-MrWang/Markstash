using Avalonia.Controls;
using FluentAvalonia.UI.Controls;
using Markstash.App.ViewModels;

namespace Markstash.App.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        if (OperatingSystem.IsWindows())
        {
            NavigationView.OpenPaneLength = 218;
            NavigationView.PaneTitle = null;
        }
    }

    private void OnBackRequested(
        object? sender,
        FANavigationViewBackRequestedEventArgs eventArgs)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.GoBack();
        }
    }
}
