using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LongLive.Mods.Models;

public sealed class LongLiveAssetMappingFile
{
    [JsonPropertyName("assets")]
    public List<LongLiveAssetMappingDefinition> Assets { get; set; } = new List<LongLiveAssetMappingDefinition>();
}
