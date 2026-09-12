using System.Text.Json.Serialization;

namespace TRoschinsky.RemoTerm;

public class Config
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = String.Empty;
    [JsonPropertyName("onlyInLockedMode")]
    public bool OnlyInLockedMode { get; set; } = false;
    [JsonPropertyName("delayActionsBy")]
    public string DelayString { get; set; } = String.Empty;
    [JsonPropertyName("actions")]
    public List<ActionConfig> Actions { get; set; } = [];

    public TimeSpan Delay { get { return CalcDelay(DelayString); } }

    private static TimeSpan CalcDelay(string delayString)
    {
        try
        {
            if (string.IsNullOrEmpty(delayString))
                return TimeSpan.Zero;

            if(delayString.Contains("random", StringComparison.OrdinalIgnoreCase))
            {
                string isMoreThanRandom = delayString.Replace("random", "", StringComparison.OrdinalIgnoreCase).Trim();
                if (isMoreThanRandom.Length > 0 && int.TryParse(isMoreThanRandom, out int maxSeconds))
                {
                    Random randCustom = new Random();
                    return TimeSpan.FromSeconds(randCustom.Next(1, maxSeconds));
                }

                Random rand = new Random();
                return TimeSpan.FromSeconds(rand.Next(1, 10));
            }

            if (int.TryParse(delayString, out int seconds))
                return TimeSpan.FromSeconds(seconds);

            if (TimeSpan.TryParse(delayString, out TimeSpan delay))
                return delay;

            return TimeSpan.Zero;
        }
        catch
        {
            return TimeSpan.Zero;
        }
    }
}