using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveModEntrypoints
{
    [JsonPropertyName("commands")]
    public string? Commands { get; set; }

    [JsonPropertyName("queries")]
    public string? Queries { get; set; }

    [JsonPropertyName("stateKeys")]
    public string? StateKeys { get; set; }

    [JsonPropertyName("content")]
    public LongLiveContentEntrypoints Content { get; set; } = new LongLiveContentEntrypoints();
}
