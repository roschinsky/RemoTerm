using System.Text.Json.Serialization;

namespace TRoschinsky.RemoTerm;

public record LogEntry(string Severity, string Message, string? Exception = null, DateTime Timestamp = default)
{
    [JsonConstructor]
    public LogEntry(string message, string? exception = null) : this("INFO", message, exception, DateTime.Now) { }
}