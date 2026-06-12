using System.Text.Json.Serialization;

namespace RemoTerm;

public class Config
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = String.Empty;
    [JsonPropertyName("onlyInLockedMode")]
    public bool OnlyInLockedMode { get; set; } = false;
    [JsonPropertyName("actions")]
    public List<ActionConfig> Actions { get; set; } = [];
}