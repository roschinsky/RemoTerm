using System;
using System.Text.Json.Serialization;

namespace TRoschinsky.RemoTerm;

public class ActionConfig
{
    [JsonPropertyName("type")]
    public ActionType ActionType {get { return actionType; } set { actionType = GetActionTypeFromString(value.ToString()); } }
    private ActionType actionType = ActionType.Unknown;

    [JsonPropertyName("name")]
    public String? Title { get; set; }
    [JsonPropertyName("payload")]
    public String Payload { get; set; } = String.Empty;

    public override string ToString()
    {
        string payload = Payload.Length > 16 ? Payload.Substring(0, 14) + "..." : Payload;
        return $"{actionType}: {payload}";
    }

    private ActionType GetActionTypeFromString(string actionType)
    {
        if(string.IsNullOrEmpty(actionType))
            return ActionType.Unknown;

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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ActionType
{
    Terminate,
    Execute,
    Message,
    Check,
    Unknown
}