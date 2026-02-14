using System.Text.Json.Serialization;

namespace CS2ServerPicker.Models;

public sealed class Preset
{
    [JsonPropertyName("presetName")]
    public string PresetName { get; set; } = string.Empty;

    [JsonPropertyName("clustered")]
    public bool Clustered { get; set; }

    [JsonPropertyName("servers")]
    public List<string> Servers { get; set; } = [];
}

public sealed class PresetCollection : Dictionary<string, Preset>
{
}
