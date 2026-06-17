using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveStateKeyFile
{
    [JsonPropertyName("keys")]
    public List<LongLiveStateKeyDefinition> Keys { get; set; } = new List<LongLiveStateKeyDefinition>();
}
