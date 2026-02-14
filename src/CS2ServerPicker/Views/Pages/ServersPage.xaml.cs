using System.Windows;
using System.Windows.Controls;
using CS2ServerPicker.ViewModels;

namespace CS2ServerPicker.Views.Pages;

public partial class ServersPage : Page
{
    private readonly ServersPageViewModel _viewModel;

    public ServersPage(ServersPageViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();
    }

    private async void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.Servers.Count == 0)
        {
            await _viewModel.LoadServersCommand.ExecuteAsync(null);
        }
    }

    private void FavoriteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ServerItemViewModel server)
        {
            _viewModel.ToggleFavoriteCommand.Execute(server);
        }
    }

    private void FilterRadio_Click(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radio && radio.Tag is string mode)
        {
            _viewModel.FilterMode = mode;
        }
    }
}
