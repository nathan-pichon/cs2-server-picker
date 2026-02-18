using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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

    private void ServerListView_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Space or Key.Enter))
            return;

        if (ServerListView.SelectedItems.Count == 0)
            return;

        int total = 0;
        int selectedCount = 0;

        foreach (var item in ServerListView.SelectedItems)
        {
            if (item is ServerItemViewModel server)
            {
                total++;
                if (server.IsSelected)
                    selectedCount++;
            }
        }

        if (total == 0)
            return;

        bool targetValue = selectedCount <= total / 2;

        foreach (var item in ServerListView.SelectedItems)
        {
            if (item is ServerItemViewModel server)
            {
                server.IsSelected = targetValue;
            }
        }

        e.Handled = true;
    }
}
