using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveItemFile
{
    [JsonPropertyName("items")]
    public List<LongLiveItemDefinition> Items { get; set; } = new List<LongLiveItemDefinition>();
}
