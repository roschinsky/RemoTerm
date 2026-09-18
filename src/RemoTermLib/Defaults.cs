using System.Net;
using System.Text.Json;

namespace TRoschinsky.Common;

/// <summary>
/// This class just gets some default values for an application. It is used 
/// to provide a consistent and centralized way to access initial default settings.
/// </summary>
public record Defaults
{
    private const string autoDiscoveryHost = "halnet.rosch-in-sky.de";
    public object? DefaultValues { get; private set; }

    public string SolutionName { get; private set; } = "<undefined>";
    public Type? DefaultType { get; private set; }
    public string Status = "<N/A>";
    public bool Success { get; private set; } = false;

    public Defaults(string? solutionName, Type defaultsType)
    {
        if(string.IsNullOrWhiteSpace(solutionName))
        {     
            Status = "No valid solution name given";
        }
        else
        {
            SolutionName = solutionName.ToLower().Trim();
            DefaultType = defaultsType ?? typeof(object);
            Success = GetDefaults();
        }
    }

    private bool GetDefaults()
    {
        bool defaultsRetrieved = false;
        var client = new HttpClient();
        
        try
        {
            Uri url = new($"http://{autoDiscoveryHost}/disco/{SolutionName}.json");
            HttpResponseMessage response = client.GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                DefaultValues = JsonSerializer.Deserialize(json, DefaultType!) ?? Activator.CreateInstance(DefaultType!);
                defaultsRetrieved = true;
            }
            else if(response.StatusCode == HttpStatusCode.NotFound)
            {
                Status = $"No defaults for '{SolutionName}' found.";
            }
            else
            {
                Status = $"Failed to retrieve defaults with status: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            Status = $"An error occurred while parsing defaults: {ex.Message}";
        }
        finally
        {
            client?.Dispose();
        }
        return defaultsRetrieved;
    }
}