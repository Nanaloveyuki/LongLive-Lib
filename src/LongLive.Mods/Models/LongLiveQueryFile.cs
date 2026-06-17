using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveQueryFile
{
    [JsonPropertyName("queries")]
    public List<LongLiveQueryDefinition> Queries { get; set; } = new List<LongLiveQueryDefinition>();
}
