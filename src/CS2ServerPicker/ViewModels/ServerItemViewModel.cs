using CommunityToolkit.Mvvm.ComponentModel;

namespace CS2ServerPicker.ViewModels;

public sealed partial class ServerItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _code = string.Empty;

    [ObservableProperty]
    private string _region = string.Empty;

    [ObservableProperty]
    private string _flagPath = string.Empty;

    [ObservableProperty]
    private List<string> _addresses = [];

    [ObservableProperty]
    private string _combinedAddresses = string.Empty;

    [ObservableProperty]
    private string _firewallRuleName = string.Empty;

    [ObservableProperty]
    private bool _isBlocked;

    [ObservableProperty]
    private bool _isFavorite;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private long _latencyMs = -1;

    [ObservableProperty]
    private string _latencyDisplay = "";

    [ObservableProperty]
    private PingStatus _pingStatus = PingStatus.Unknown;

    [ObservableProperty]
    private bool _isPinging;
}

public enum PingStatus
{
    Unknown,
    Pinging,
    Success,
    TimedOut,
    Blocked,
    Error
}
