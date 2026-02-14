using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CS2ServerPicker.Models;
using CS2ServerPicker.Services;

namespace CS2ServerPicker.ViewModels;

public sealed partial class PresetsPageViewModel : ObservableObject
{
    private readonly IPresetService _presetService;
    private readonly ServersPageViewModel _serversVm;
    private readonly AppSettings _settings;
    private PresetCollection _allPresets = [];

    public PresetsPageViewModel(
        IPresetService presetService,
        ServersPageViewModel serversVm,
        AppSettings settings)
    {
        _presetService = presetService;
        _serversVm = serversVm;
        _settings = settings;
    }

    public ObservableCollection<PresetItemViewModel> Presets { get; } = [];
    public ObservableCollection<string> SelectedPresetServers { get; } = [];
    public ObservableCollection<string> AvailableServers { get; } = [];

    [ObservableProperty]
    private PresetItemViewModel? _selectedPreset;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private bool _isAddingPreset;

    [ObservableProperty]
    private bool _isEditingPreset;

    [ObservableProperty]
    private string _newPresetName = "";

    [ObservableProperty]
    private ObservableCollection<ServerSelectionItem> _serverSelectionItems = [];

    partial void OnSelectedPresetChanged(PresetItemViewModel? value)
    {
        LoadPresetServers(value);
    }

    [RelayCommand]
    private async Task LoadPresetsAsync()
    {
        _allPresets = await _presetService.LoadPresetsAsync();
        RefreshPresetList();
        RefreshAvailableServers();
    }

    [RelayCommand]
    private void ShowAddPreset()
    {
        IsAddingPreset = true;
        IsEditingPreset = false;
        NewPresetName = "";

        ServerSelectionItems.Clear();
        foreach (var server in _serversVm.Servers)
        {
            ServerSelectionItems.Add(new ServerSelectionItem
            {
                Name = server.Name,
                IsSelected = false
            });
        }
    }

    [RelayCommand]
    private void ShowEditPreset()
    {
        if (SelectedPreset is null) return;

        IsEditingPreset = true;
        IsAddingPreset = false;
        NewPresetName = SelectedPreset.Name;

        var presetServers = new HashSet<string>(SelectedPresetServers);

        ServerSelectionItems.Clear();
        foreach (var server in _serversVm.Servers)
        {
            ServerSelectionItems.Add(new ServerSelectionItem
            {
                Name = server.Name,
                IsSelected = presetServers.Contains(server.Name)
            });
        }
    }

    [RelayCommand]
    private async Task SavePresetAsync()
    {
        if (string.IsNullOrWhiteSpace(NewPresetName))
        {
            StatusText = "Preset name cannot be empty.";
            return;
        }

        if (Regex.IsMatch(NewPresetName, @"[^a-zA-Z0-9 ]"))
        {
            StatusText = "Preset name can only contain letters, numbers, and spaces.";
            return;
        }

        var selectedServers = ServerSelectionItems
            .Where(s => s.IsSelected)
            .Select(s => s.Name)
            .ToList();

        if (selectedServers.Count == 0)
        {
            StatusText = "Select at least one server.";
            return;
        }

        try
        {
            if (IsAddingPreset)
            {
                await _presetService.AddPresetAsync(
                    NewPresetName, _settings.IsClustered, selectedServers);
                StatusText = $"Preset '{NewPresetName}' created.";
            }
            else if (IsEditingPreset && SelectedPreset is not null)
            {
                await _presetService.UpdatePresetAsync(
                    SelectedPreset.Key, NewPresetName, _settings.IsClustered, selectedServers);
                StatusText = $"Preset '{NewPresetName}' updated.";
            }

            IsAddingPreset = false;
            IsEditingPreset = false;
            await LoadPresetsAsync();
        }
        catch (InvalidOperationException ex)
        {
            StatusText = ex.Message;
        }
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsAddingPreset = false;
        IsEditingPreset = false;
    }

    [RelayCommand]
    private async Task DeletePresetAsync()
    {
        if (SelectedPreset is null) return;

        await _presetService.DeletePresetAsync(SelectedPreset.Key);
        StatusText = $"Preset '{SelectedPreset.Name}' deleted.";
        await LoadPresetsAsync();
    }

    [RelayCommand]
    private async Task ApplyBlockPresetAsync()
    {
        if (SelectedPreset is null || SelectedPresetServers.Count == 0) return;

        await _serversVm.ApplyPresetAsync([.. SelectedPresetServers], blockExcept: false);
        StatusText = $"Applied preset '{SelectedPreset.Name}' - blocking selected servers.";
    }

    [RelayCommand]
    private async Task ApplyBlockExceptPresetAsync()
    {
        if (SelectedPreset is null || SelectedPresetServers.Count == 0) return;

        await _serversVm.ApplyPresetAsync([.. SelectedPresetServers], blockExcept: true);
        StatusText = $"Applied preset '{SelectedPreset.Name}' - blocking all except selected.";
    }

    [RelayCommand]
    private async Task ExportPresetsAsync()
    {
        // In a real app, this would show a SaveFileDialog
        // For now, export to a known location
        var path = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "cs2_presets_export.json");

        await _presetService.ExportPresetsAsync(_allPresets, path);
        StatusText = $"Presets exported to {path}";
    }

    private void RefreshPresetList()
    {
        Presets.Clear();
        foreach (var (key, preset) in _allPresets)
        {
            if (preset.Clustered != _settings.IsClustered)
                continue;

            Presets.Add(new PresetItemViewModel
            {
                Key = key,
                Name = preset.PresetName,
                ServerCount = preset.Servers.Count,
                IsClustered = preset.Clustered
            });
        }
    }

    private void RefreshAvailableServers()
    {
        AvailableServers.Clear();
        foreach (var server in _serversVm.Servers)
            AvailableServers.Add(server.Name);
    }

    private void LoadPresetServers(PresetItemViewModel? preset)
    {
        SelectedPresetServers.Clear();

        if (preset is null) return;

        if (_allPresets.TryGetValue(preset.Key, out var presetData))
        {
            foreach (var server in presetData.Servers)
                SelectedPresetServers.Add(server);
        }
    }
}

public sealed partial class PresetItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private int _serverCount;

    [ObservableProperty]
    private bool _isClustered;
}

public sealed partial class ServerSelectionItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
