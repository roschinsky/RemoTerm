using System;

namespace RemoTerm;

public class ActionConfig
{
    public ActionType ActionType {get; set;}
    public String? Title { get; set; }
    public String Payload { get; set; } = String.Empty;

    public override string ToString()
    {
        string payload = Payload.Length > 16 ? Payload.Substring(0, 14) + "..." : Payload;
        return $"{ActionType}: {payload}";
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