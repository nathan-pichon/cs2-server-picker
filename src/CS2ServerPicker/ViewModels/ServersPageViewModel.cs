using System.Collections.ObjectModel;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CS2ServerPicker.Configuration;
using CS2ServerPicker.Constants;
using CS2ServerPicker.Models;
using CS2ServerPicker.Services;
using CS2ServerPicker.Services.Filtering;
using CS2ServerPicker.Services.Settings;
using CS2ServerPicker.Services.Sorting;

namespace CS2ServerPicker.ViewModels;

public sealed partial class ServersPageViewModel : ObservableObject
{
    private readonly IServerDataService _serverDataService;
    private readonly IFirewallService _firewallService;
    private readonly IPingService _pingService;
    private readonly IServerFilterService _filterService;
    private readonly IServerSortService _sortService;
    private readonly ISettingsRepository _settingsRepository;
    private readonly AppSettings _settings;

    private List<ServerInfo> _allServers = [];
    private Dictionary<string, ClusteredServer> _clusteredServers = [];
    private CancellationTokenSource? _pingCancellationSource;
    private DispatcherTimer? _autoRefreshTimer;
    private bool _isPingInProgress;

    /// <summary>
    /// Ping cache keyed by individual server Name.
    /// Populated during any ping pass and used to compute cluster averages
    /// without re-pinging when the user changes filter, sort, or search text.
    /// </summary>
    private readonly Dictionary<string, (long LatencyMs, PingStatus Status, string Display)> _pingCache = [];

    public ServersPageViewModel(
        IServerDataService serverDataService,
        IFirewallService firewallService,
        IPingService pingService,
        IServerFilterService filterService,
        IServerSortService sortService,
        ISettingsRepository settingsRepository,
        AppSettings settings)
    {
        _serverDataService = serverDataService;
        _firewallService = firewallService;
        _pingService = pingService;
        _filterService = filterService;
        _sortService = sortService;
        _settingsRepository = settingsRepository;
        _settings = settings;
        _isClustered = settings.IsClustered;
    }

    // ─── Collections ────────────────────────────────────────────────────────────

    public ObservableCollection<ServerItemViewModel> Servers { get; } = [];
    public ObservableCollection<ServerItemViewModel> FilteredServers { get; } = [];

    // ─── Observable properties ──────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private SortColumn _sortColumn = SortColumn.Name;

    [ObservableProperty]
    private bool _sortAscending = true;

    [ObservableProperty]
    private bool _isClustered;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _selectedRegionFilter = "All";

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private bool _isOperationInProgress;

    [ObservableProperty]
    private bool _autoRefreshEnabled;

    [ObservableProperty]
    private int _autoRefreshInterval = 60;

    [ObservableProperty]
    private string _filterMode = "All"; // All, Favorites, Blocked

    public string[] RegionFilters { get; } = AppConstants.Regions.FilterOptions;

    // ─── Property change reactions ──────────────────────────────────────────────

    partial void OnSearchTextChanged(string value) => RefreshFilteredServerList();
    partial void OnSelectedRegionFilterChanged(string value) => RefreshFilteredServerList();
    partial void OnFilterModeChanged(string value) => RefreshFilteredServerList();
    partial void OnSortColumnChanged(SortColumn value) => RefreshFilteredServerList();
    partial void OnSortAscendingChanged(bool value) => RefreshFilteredServerList();

    partial void OnAutoRefreshEnabledChanged(bool value)
    {
        if (value)
            StartAutoRefresh();
        else
            StopAutoRefresh();
    }

    // ─── Commands ───────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task LoadServersAsync()
    {
        IsLoading = true;
        StatusText = "Fetching server data from Steam...";

        var fetchedServerData = await _serverDataService.FetchServerDataAsync();

        if (fetchedServerData is null)
        {
            StatusText = "Failed to load server data. Check your internet connection.";
            IsLoading = false;
            return;
        }

        var (revision, servers) = fetchedServerData.Value;
        _allServers = servers;
        _clusteredServers = _serverDataService.BuildClusteredView(servers);

        await UnblockAllServersIfRevisionChangedAsync(revision);

        _settings.ServerRevision = revision;
        _settingsRepository.Save(_settings);

        PopulateServerList();
        IsLoading = false;
        StatusText = $"Loaded {Servers.Count} servers";

        await PingAllServersAsync();
    }

    [RelayCommand]
    private async Task RefreshPingsAsync()
    {
        if (IsOperationInProgress) return;

        _pingCache.Clear();
        await PingAllServersAsync();
    }

    [RelayCommand]
    private async Task ToggleClusterAsync()
    {
        if (IsOperationInProgress) return;

        StatusText = "Unblocking all servers before changing view...";
        await UnblockAllServersAsync();

        _pingCache.Clear();

        IsClustered = !IsClustered;
        _settings.IsClustered = IsClustered;
        _settingsRepository.Save(_settings);

        PopulateServerList();
        StatusText = IsClustered ? "Clustered view enabled" : "Unclustered view enabled";

        await PingAllServersAsync();
    }

    [RelayCommand]
    private async Task BlockSelectedAsync()
    {
        if (IsOperationInProgress) return;

        var selectedServers = FilteredServers.Where(server => server.IsSelected).ToList();
        if (selectedServers.Count == 0)
        {
            StatusText = "No servers selected.";
            return;
        }

        IsOperationInProgress = true;
        StatusText = $"Blocking {selectedServers.Count} server(s)...";

        foreach (var server in selectedServers)
            await BlockServerAsync(server);

        IsOperationInProgress = false;
        StatusText = $"Blocked {selectedServers.Count} server(s)";
    }

    [RelayCommand]
    private async Task UnblockSelectedAsync()
    {
        if (IsOperationInProgress) return;

        var selectedServers = FilteredServers.Where(server => server.IsSelected).ToList();
        if (selectedServers.Count == 0)
        {
            StatusText = "No servers selected.";
            return;
        }

        IsOperationInProgress = true;
        StatusText = $"Unblocking {selectedServers.Count} server(s)...";

        foreach (var server in selectedServers)
            await UnblockServerAsync(server);

        IsOperationInProgress = false;
        StatusText = $"Unblocked {selectedServers.Count} server(s)";

        await PingServersAsync(selectedServers);
    }

    [RelayCommand]
    private async Task BlockAllAsync()
    {
        if (IsOperationInProgress) return;

        IsOperationInProgress = true;
        _pingService.CancelAll();
        StatusText = "Blocking all servers...";

        foreach (var server in Servers)
            await BlockServerAsync(server);

        IsOperationInProgress = false;
        StatusText = "All servers blocked";
    }

    [RelayCommand]
    private async Task UnblockAllAsync()
    {
        if (IsOperationInProgress) return;

        IsOperationInProgress = true;
        _pingService.CancelAll();
        StatusText = "Unblocking all servers...";

        await UnblockAllServersAsync();

        IsOperationInProgress = false;
        StatusText = "All servers unblocked";

        _pingCache.Clear();
        await PingAllServersAsync();
    }

    [RelayCommand]
    private void ToggleFavorite(ServerItemViewModel server)
    {
        server.IsFavorite = !server.IsFavorite;

        if (server.IsFavorite)
            _settings.FavoriteServers.Add(server.Name);
        else
            _settings.FavoriteServers.Remove(server.Name);

        _settingsRepository.Save(_settings);
        RefreshFilteredServerList();
    }

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var server in FilteredServers)
            server.IsSelected = true;
    }

    [RelayCommand]
    private void DeselectAll()
    {
        foreach (var server in FilteredServers)
            server.IsSelected = false;
    }

    [RelayCommand]
    private void Sort(string columnName)
    {
        var column = Enum.TryParse<SortColumn>(columnName, ignoreCase: true, out var parsed)
            ? parsed
            : SortColumn.Name;

        if (SortColumn == column)
            SortAscending = !SortAscending;
        else
        {
            SortColumn = column;
            SortAscending = true;
        }
    }

    // ─── Public API used by PresetsPageViewModel ─────────────────────────────────

    public async Task ApplyPresetAsync(string[] presetServerNames, bool blockExcept)
    {
        if (IsOperationInProgress) return;

        IsOperationInProgress = true;
        _pingService.CancelAll();
        await UnblockAllServersAsync();

        var presetServerNameSet = new HashSet<string>(presetServerNames);

        foreach (var server in Servers)
        {
            bool shouldBlockThisServer = blockExcept
                ? !presetServerNameSet.Contains(server.Name)
                : presetServerNameSet.Contains(server.Name);

            if (shouldBlockThisServer)
                await BlockServerAsync(server);
        }

        IsOperationInProgress = false;
        StatusText = "Preset applied";

        _pingCache.Clear();
        await PingAllServersAsync();
    }

    public async Task PingOneServerAsync(ServerItemViewModel server)
    {
        if (server.IsBlocked)
        {
            MarkServerAsBlocked(server);
            return;
        }

        MarkServerAsPinging(server);

        var pingResult = await _pingService.PingServerAsync(server.Addresses);
        UpdateServerWithPingResult(server, pingResult);
    }

    // ─── Cleanup ─────────────────────────────────────────────────────────────────

    public void Cleanup()
    {
        StopAutoRefresh();
        CancelAndDisposePingCancellationSource();
        _pingService.CancelAll();
    }

    // ─── Private server list management ──────────────────────────────────────────

    private void PopulateServerList()
    {
        Servers.Clear();

        if (IsClustered)
            PopulateWithClusteredServers();
        else
            PopulateWithIndividualServers();

        RefreshFilteredServerList();
    }

    private void PopulateWithClusteredServers()
    {
        // Only show entries that are defined clusters — BuildClusteredView also adds individual
        // non-clustered servers to the dictionary, which must be excluded in cluster view.
        var clusterNameSet = ClusterConfiguration.Definitions
            .Select(definition => definition.ClusterName)
            .ToHashSet();

        foreach (var (clusterName, cluster) in _clusteredServers)
        {
            if (!clusterNameSet.Contains(clusterName)) continue;

            var flagNameOverride = ClusterConfiguration.Definitions
                .FirstOrDefault(definition => definition.ClusterName == clusterName)
                ?.FlagName;

            Servers.Add(CreateServerItemViewModel(
                clusterName, cluster.AllAddresses, cluster.Region,
                cluster.FirewallRuleName, flagNameOverride));
        }
    }

    private void PopulateWithIndividualServers()
    {
        foreach (var server in _allServers)
        {
            Servers.Add(CreateServerItemViewModel(
                server.Name, server.RelayAddresses, server.Region,
                server.FirewallRuleName));
        }
    }

    private ServerItemViewModel CreateServerItemViewModel(
        string name, List<string> addresses, string region,
        string firewallRuleName, string? flagNameOverride = null)
    {
        return new ServerItemViewModel
        {
            Name = name,
            Region = region,
            Addresses = addresses,
            CombinedAddresses = string.Join(",", addresses),
            FirewallRuleName = firewallRuleName,
            FlagPath = FlagResolver.GetFlagPackUri(name, flagNameOverride),
            IsFavorite = _settings.FavoriteServers.Contains(name)
        };
    }

    // ─── Private filter/sort coordination ────────────────────────────────────────

    private void RefreshFilteredServerList()
    {
        FilteredServers.Clear();

        var filteredServers = _filterService.Apply(Servers, SearchText, SelectedRegionFilter, FilterMode);
        var sortedServers = _sortService.Apply(filteredServers, SortColumn, SortAscending);

        foreach (var server in sortedServers)
            FilteredServers.Add(server);
    }

    // ─── Private firewall operations ──────────────────────────────────────────────

    private async Task BlockServerAsync(ServerItemViewModel server)
    {
        await _firewallService.BlockServerAsync(server.FirewallRuleName, server.CombinedAddresses);
        MarkServerAsBlocked(server);
    }

    private async Task UnblockServerAsync(ServerItemViewModel server)
    {
        await _firewallService.UnblockServerAsync(server.FirewallRuleName);
        MarkServerAsUnblocked(server);
    }

    private async Task UnblockAllServersAsync()
    {
        foreach (var server in Servers)
            await UnblockServerAsync(server);
    }

    private async Task UnblockAllServersIfRevisionChangedAsync(string newRevision)
    {
        bool serverDataWasUpdatedByValve =
            !string.IsNullOrEmpty(_settings.ServerRevision) &&
            _settings.ServerRevision != newRevision;

        if (serverDataWasUpdatedByValve)
        {
            StatusText = "Server data updated by Valve. Unblocking all servers...";
            await UnblockAllServersAsync();
        }
    }

    // ─── Private ping operations ──────────────────────────────────────────────────

    private async Task PingAllServersAsync()
    {
        var cancellationToken = ResetPingCancellationSource();

        StatusText = "Pinging all servers...";

        if (IsClustered)
            await PingClustersViaMemberServersAsync(cancellationToken);
        else
            await PingIndividualServersAsync(cancellationToken);

        if (!cancellationToken.IsCancellationRequested)
            StatusText = "Ready";
    }

    /// <summary>
    /// In cluster view: pings every individual server from <see cref="_allServers"/>,
    /// caches each result, then computes and applies the average latency per cluster.
    /// This avoids pinging the combined cluster address list directly and gives a more
    /// accurate per-region latency reading from all member relays.
    /// </summary>
    private async Task PingClustersViaMemberServersAsync(CancellationToken cancellationToken)
    {
        foreach (var clusterViewModel in Servers)
            MarkServerAsPinging(clusterViewModel);

        foreach (var individualServer in _allServers.ToList())
        {
            if (cancellationToken.IsCancellationRequested) break;

            var isBlocked = await _firewallService.IsServerBlockedAsync(
                individualServer.FirewallRuleName, cancellationToken);

            if (isBlocked)
            {
                _pingCache[individualServer.Name] = (-1L, PingStatus.Blocked, "Blocked");
            }
            else
            {
                var pingResult = await _pingService.PingServerAsync(
                    individualServer.RelayAddresses, cancellationToken);

                _pingCache[individualServer.Name] = pingResult switch
                {
                    { Success: true }  => (pingResult.LatencyMs, PingStatus.Success, $"{pingResult.LatencyMs}ms"),
                    { TimedOut: true } => (-1L, PingStatus.TimedOut, "Timed out"),
                    _                  => (-1L, PingStatus.Error, "Error")
                };
            }
        }

        if (cancellationToken.IsCancellationRequested) return;

        foreach (var clusterViewModel in Servers.ToList())
            ApplyCachedAveragePingToCluster(clusterViewModel);
    }

    /// <summary>
    /// In individual view: pings each visible server and caches the result.
    /// </summary>
    private async Task PingIndividualServersAsync(CancellationToken cancellationToken)
    {
        foreach (var server in Servers.ToList())
        {
            if (cancellationToken.IsCancellationRequested) break;

            var isBlocked = await _firewallService.IsServerBlockedAsync(
                server.FirewallRuleName, cancellationToken);

            server.IsBlocked = isBlocked;

            if (isBlocked)
                MarkServerAsBlocked(server);
            else
                await PingOneServerAsync(server);
        }
    }

    /// <summary>
    /// Reads member server pings from <see cref="_pingCache"/>, averages successful ones,
    /// and applies the result to the cluster's <see cref="ServerItemViewModel"/>.
    /// Falls back to Blocked or TimedOut when no successful pings are available.
    /// </summary>
    private void ApplyCachedAveragePingToCluster(ServerItemViewModel clusterViewModel)
    {
        if (!_clusteredServers.TryGetValue(clusterViewModel.Name, out var cluster))
            return;

        var successfulMemberPings = cluster.MemberServerNames
            .Where(memberName =>
                _pingCache.TryGetValue(memberName, out var cached) && cached.LatencyMs > 0)
            .Select(memberName => _pingCache[memberName].LatencyMs)
            .ToList();

        clusterViewModel.IsPinging = false;

        if (successfulMemberPings.Count > 0)
        {
            var averageLatencyMs = (long)successfulMemberPings.Average();
            clusterViewModel.LatencyMs = averageLatencyMs;
            clusterViewModel.LatencyDisplay = $"{averageLatencyMs}ms";
            clusterViewModel.PingStatus = PingStatus.Success;
            return;
        }

        var allMembersAreBlocked = cluster.MemberServerNames.All(memberName =>
            _pingCache.TryGetValue(memberName, out var cached) &&
            cached.Status == PingStatus.Blocked);

        if (allMembersAreBlocked)
        {
            MarkServerAsBlocked(clusterViewModel);
        }
        else
        {
            clusterViewModel.LatencyMs = -1;
            clusterViewModel.LatencyDisplay = "Timed out";
            clusterViewModel.PingStatus = PingStatus.TimedOut;
        }
    }

    private async Task PingServersAsync(IEnumerable<ServerItemViewModel> servers)
    {
        foreach (var server in servers)
            await PingOneServerAsync(server);
    }

    private CancellationToken ResetPingCancellationSource()
    {
        CancelAndDisposePingCancellationSource();
        _pingCancellationSource = new CancellationTokenSource();
        return _pingCancellationSource.Token;
    }

    private void CancelAndDisposePingCancellationSource()
    {
        _pingCancellationSource?.Cancel();
        _pingCancellationSource?.Dispose();
        _pingCancellationSource = null;
    }

    // ─── Private server state helpers ────────────────────────────────────────────

    private void MarkServerAsBlocked(ServerItemViewModel server)
    {
        server.IsBlocked = true;
        server.PingStatus = PingStatus.Blocked;
        server.LatencyDisplay = "Blocked";
        _pingCache[server.Name] = (-1L, PingStatus.Blocked, "Blocked");
    }

    private static void MarkServerAsUnblocked(ServerItemViewModel server)
    {
        server.IsBlocked = false;
        server.PingStatus = PingStatus.Unknown;
        server.LatencyDisplay = string.Empty;
    }

    private static void MarkServerAsPinging(ServerItemViewModel server)
    {
        server.IsPinging = true;
        server.PingStatus = PingStatus.Pinging;
        server.LatencyDisplay = "Pinging...";
    }

    private void UpdateServerWithPingResult(ServerItemViewModel server, PingResult pingResult)
    {
        server.IsPinging = false;

        if (pingResult.Success)
        {
            server.LatencyMs = pingResult.LatencyMs;
            server.LatencyDisplay = $"{pingResult.LatencyMs}ms";
            server.PingStatus = PingStatus.Success;
            _pingCache[server.Name] = (pingResult.LatencyMs, PingStatus.Success, $"{pingResult.LatencyMs}ms");
        }
        else if (pingResult.TimedOut)
        {
            server.LatencyMs = -1;
            server.LatencyDisplay = "Timed out";
            server.PingStatus = PingStatus.TimedOut;
            _pingCache[server.Name] = (-1L, PingStatus.TimedOut, "Timed out");
        }
        else
        {
            server.LatencyMs = -1;
            server.LatencyDisplay = "Error";
            server.PingStatus = PingStatus.Error;
            _pingCache[server.Name] = (-1L, PingStatus.Error, "Error");
        }
    }

    // ─── Auto-refresh timer ───────────────────────────────────────────────────────

    private void StartAutoRefresh()
    {
        StopAutoRefresh();

        if (AutoRefreshInterval <= 0) return;

        _autoRefreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(AutoRefreshInterval)
        };
        _autoRefreshTimer.Tick += OnAutoRefreshTimerTick;
        _autoRefreshTimer.Start();
    }

    private void StopAutoRefresh()
    {
        _autoRefreshTimer?.Stop();
        _autoRefreshTimer = null;
    }

    private async void OnAutoRefreshTimerTick(object? sender, EventArgs e)
    {
        if (_isPingInProgress) return;

        try
        {
            _isPingInProgress = true;
            await PingAllServersAsync();
        }
        finally
        {
            _isPingInProgress = false;
        }
    }
}
