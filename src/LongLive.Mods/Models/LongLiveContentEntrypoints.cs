using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveContentEntrypoints
{
    [JsonPropertyName("items")]
    public string? Items { get; set; }

    [JsonPropertyName("skills")]
    public string? Skills { get; set; }

    [JsonPropertyName("buffs")]
    public string? Buffs { get; set; }

    [JsonPropertyName("assets")]
    public string? Assets { get; set; }
}
