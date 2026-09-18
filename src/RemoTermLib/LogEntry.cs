using System.Text.Json.Serialization;

namespace TRoschinsky.RemoTerm;

public record LogEntry(string Severity, string Message, Exception? Exception = null, DateTime Timestamp = default)
{
    [JsonConstructor]
    public LogEntry(string message, Exception? exception = null) : this("INFO", message, exception, DateTime.Now) { }
}