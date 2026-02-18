using System.Windows.Controls;
using CS2ServerPicker.ViewModels;

namespace CS2ServerPicker.Views.Pages;

public partial class SettingsPage : Page
{
    public SettingsPage(SettingsViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }
}
