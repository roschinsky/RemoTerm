using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json.Serialization;

namespace TRoschinsky.RemoTerm;

public class Updater
{
    private readonly HttpClient httpClient;
    private bool checkForNewerVersion = true;
    private bool runInstallerOnUpdate = false;
    private Version? currentVersion;

    public Updater(bool forceUpdate = false, bool forceInstaller = false)
    {
        httpClient = new HttpClient();
        try
        {
            currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            checkForNewerVersion = !forceUpdate;
            runInstallerOnUpdate = forceInstaller;
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("RemoTerm.Updater", "1.0"));
            Task update = CheckAndApplyUpdateAsync();
        }
        catch (Exception) { }
    }

    public async Task CheckAndApplyUpdateAsync()
    {
        try
        {
            string url = $"https://api.github.com/repos/roschinsky/RemoTerm/releases/latest";
            var release = await httpClient.GetFromJsonAsync<GitHubRelease>(url);

            if (release == null || string.IsNullOrEmpty(release.TagName))
            {
                Console.WriteLine("No release information found.");
                return;
            }
            // Normalize versions (e.g., stripping 'v' if tag is v1.0.0)
            var latestVersion = Version.Parse(release.TagName.TrimStart('v'));

            if (latestVersion > currentVersion || !checkForNewerVersion)
            {
                // Find the asset that ends with .zip
                var zipAsset = Array.Find(release.Assets, a => a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));
                if (zipAsset == null) return;

                // 1. Download the ZIP file to a temp directory
                string tempZipPath = Path.Combine(Path.GetTempPath(), zipAsset.Name);
                byte[] fileBytes = await httpClient.GetByteArrayAsync(zipAsset.BrowserDownloadUrl);
                await File.WriteAllBytesAsync(tempZipPath, fileBytes);

                // 2. Execute the self-overwriting update script
                ApplyZipUpdateAndRestart(tempZipPath, "RemoTerm.exe");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Retrieval of update failed: {ex.Message}");
        }
    }

    private void ApplyZipUpdateAndRestart(string zipPath, string exeToRun)
    {
        try
        {
            string currentAppDir = AppContext.BaseDirectory;
            string tempExtractDir = Path.Combine(Path.GetTempPath(), "RtUpd");

            // Clean old extraction directory if it exists
            if (Directory.Exists(tempExtractDir)) Directory.Delete(tempExtractDir, true);

            // Extract the downloaded zip to a temporary folder
            ZipFile.ExtractToDirectory(zipPath, tempExtractDir);

            // Generate a temporary batch file to handle the file swapping while this app closes
            string scriptPath = Path.Combine(Path.GetTempPath(), "RtUpdApply.bat");
            string launchInstaller = runInstallerOnUpdate ? $"start \"\" \"{Path.Combine(currentAppDir, exeToRun)}\" -i" : string.Empty;

            string batchScript = $@"@echo off
timeout /t 5 /nobreak > nul
xcopy ""{tempExtractDir}\*"" ""{currentAppDir}"" /s /e /y /q
{launchInstaller}
rmdir /s /q ""{tempExtractDir}""
del ""{zipPath}""
del ""%~f0""
";

            File.WriteAllText(scriptPath, batchScript);

            // 3. Launch the batch script silently in the background
            ProcessStartInfo startInfo = new()
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{scriptPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false
            };

            Process.Start(startInfo);

            // 4. Immediately kill the main app so xcopy doesn't hit a "File in Use" exception
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Initializing update failed: {ex.Message}");
        }
    }
}

// GitHub API JSON mapping models
public record GitHubRelease(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("tag_name")] string TagName,
    [property: JsonPropertyName("assets")] GitHubAsset[] Assets
);

public record GitHubAsset(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl
);
