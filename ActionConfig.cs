using System;
using System.Text.Json.Serialization;

namespace TRoschinsky.RemoTerm;

public class ActionConfig
{
    public ActionType ActionType {get { return GetActionTypeFromString(Type); }}

    [JsonPropertyName("type")]
    public String Type { private get; set; } = String.Empty;
    [JsonPropertyName("name")]
    public String? Title { get; set; }
    [JsonPropertyName("payload")]
    public String Payload { get; set; } = String.Empty;

    public override string ToString()
    {
        string payload = Payload.Length > 16 ? Payload.Substring(0, 14) + "..." : Payload;
        return $"{ActionType}: {payload}";
    }

    private ActionType GetActionTypeFromString(string actionType)
    {
        return actionType.ToLower() switch
        {
            "terminate" => ActionType.Terminate,
            "execute" => ActionType.Execute,
            "message" => ActionType.Message,
            "check" => ActionType.Check,
            _ => ActionType.Unknown
        };
    }
}

public enum ActionType
{
    Terminate,
    Execute,
    Message,
    Check,
    Unknown
}