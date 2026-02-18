using CommunityToolkit.Mvvm.ComponentModel;

namespace CS2ServerPicker.ViewModels;

public sealed partial class MainWindowViewModel : ObservableObject
{
    public MainWindowViewModel(
        ServersPageViewModel serversVm,
        PresetsPageViewModel presetsVm,
        SettingsViewModel settingsVm)
    {
        ServersVm = serversVm;
        PresetsVm = presetsVm;
        SettingsVm = settingsVm;
    }

    public ServersPageViewModel ServersVm { get; }
    public PresetsPageViewModel PresetsVm { get; }
    public SettingsViewModel SettingsVm { get; }

    [ObservableProperty]
    private string _applicationTitle = "CS2 Server Picker";
}
