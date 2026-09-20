using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;

namespace TRoschinsky.RemoTerm;

public class Installer 
{
    public static string? InstallPath { get; private set; }
    public static string? SourcePath { get; private set; }
    
    public string Status { get; private set; } = "Not started";
    public bool IsInstalled { get; private set; } = false;

    public Installer(string id, bool isDebug = false, string customInstallPath = "")
    {
        if(isDebug)
        {
            MessageBox.Show($"Triggered installation with config #{id}!{Environment.NewLine}Click okay to continue.", "Installer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        if(string.IsNullOrEmpty(id))
        {            
            Status = "Installation failed: Config ID is required.";
            Environment.ExitCode = 10;
            return;
        }

        if(!string.IsNullOrEmpty(customInstallPath))
        {
            InstallPath = customInstallPath.Trim();
        }

        IsInstalled = RunInstallation(id);

        if(isDebug)
        {
            if(IsInstalled)
            {
                MessageBox.Show($"Installation completed successfully with config #{id}!{Environment.NewLine}Status: {Status}", "Installer", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Installation failed with config #{id}!{Environment.NewLine}Status: {Status}", "Installer", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public bool RunInstallation(string id)
    {
        try
        {
            if(!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                throw new Exception("Installation requires administrator privileges!");
            }

            bool installSuccess = InstallRemoTerm();            
            bool scheduledTaskSuccess = SetupScheduledTask(id);
            Status = "Installation completed successfully!";
            Environment.ExitCode = 0;
            return true;
        }
        catch (Exception ex)
        {
            Status = $"Installation failed due to {ex.Message}";
            Environment.ExitCode = 11;
            return false;
        }
    }

    private bool InstallRemoTerm()
    {
        try
        {
            string currentLocation = Assembly.GetExecutingAssembly().Location;
            SourcePath = Path.GetDirectoryName(currentLocation);

            if (string.IsNullOrEmpty(SourcePath))
            {
                throw new Exception("Source path could not be determined.");
            }

            if (string.IsNullOrEmpty(InstallPath))
            {
                InstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Common Files", "Microsoft Shared", "RT");
            }

            if (!Directory.Exists(InstallPath))
            {
                Directory.CreateDirectory(InstallPath);
            }

            foreach (var file in Directory.GetFiles(SourcePath))
            {
                var destFile = Path.Combine(InstallPath, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Installation failed: {ex.Message}");
        }
    }

    private bool SetupScheduledTask(string id)
    {
        const string taskFileName = "RemoTermSchedTask.xml";

        try
        {
            if (string.IsNullOrEmpty(SourcePath) || string.IsNullOrEmpty(InstallPath))
            {
                throw new Exception("Source or install path is not set.");
            }

            string taskFilePath = Path.Combine(SourcePath, taskFileName);
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            if(!Array.Exists(resourceNames, name => name.EndsWith(taskFileName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new FileNotFoundException($"Embedded resource '{taskFileName}' not found in assembly.");
            }
            using (Stream? resourceStream = assembly.GetManifestResourceStream($"RemoTerm.{taskFileName}"))
            {
                if (resourceStream == null)
                {
                    throw new FileNotFoundException($"Embedded payload '{taskFileName}' not found.");
                }


                using (FileStream fileStream = new FileStream(taskFilePath, FileMode.Create, FileAccess.Write))
                {
                    resourceStream.CopyTo(fileStream);
                    fileStream.Flush();
                }
            }

            if (!File.Exists(taskFilePath))
            {
                throw new FileNotFoundException("Scheduled task XML file not found.", taskFilePath);
            }

            string xmlContent = File.ReadAllText(taskFilePath);
            xmlContent = xmlContent.Replace("{{EXECUTABLE_PATH}}", Path.Combine(InstallPath, "RemoTerm.exe"));
            xmlContent = xmlContent.Replace("{{CONFIG_ID}}", id);

            string taskFilePathNew = Path.Combine(SourcePath, taskFileName.Replace(".xml", $"_{id}.xml"));
            File.WriteAllText(taskFilePathNew, xmlContent);

            var processInfo = new ProcessStartInfo
            {
                FileName = "schtasks",
                Arguments = $"/Create /TN \"RT\" /XML \"{taskFilePathNew}\" /F",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(processInfo))
            {
                process?.WaitForExit();
                if (process?.ExitCode != 0)
                {
                    string errorOutput = process!.StandardError.ReadToEnd();
                    throw new Exception($"Failed to create scheduled task. Error: {errorOutput}");
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            throw new Exception($"Scheduled task setup failed: {ex.Message}");
        }
    }
}