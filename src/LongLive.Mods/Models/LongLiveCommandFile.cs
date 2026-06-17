using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveCommandFile
{
    [JsonPropertyName("commands")]
    public List<LongLiveCommandDefinition> Commands { get; set; } = new List<LongLiveCommandDefinition>();
}
