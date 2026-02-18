using System.Windows;
using System.Windows.Controls;
using CS2ServerPicker.ViewModels;

namespace CS2ServerPicker.Views.Pages;

public partial class PresetsPage : Page
{
    private readonly PresetsPageViewModel _viewModel;

    public PresetsPage(PresetsPageViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadPresetsCommand.ExecuteAsync(null);
    }
}
