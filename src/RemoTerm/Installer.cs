namespace TRoschinsky.RemoTerm;

public class Installer 
{
    public static string InstallPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "RT");
    public string Status { get; private set; } = "Not started";
    public bool IsInstalled { get; private set; } = false;

    public Installer(string id, bool isDebug = false, string customInstallPath = "")
    {
        if(isDebug)
        {
            MessageBox.Show($"Installation triggered successfully with config #{id}!{Environment.NewLine}Click okay to continue.", "Installer", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        if(!string.IsNullOrEmpty(customInstallPath))
        {
            InstallPath = customInstallPath.Trim();
        }

        IsInstalled = RunInstallation(id);
    }

    public bool RunInstallation(string id)
    {
        try
        {
            bool installSuccess = InstallRemoTerm();
            bool scheduledTaskSuccess = SetupScheduledTask(id);
            Status = "Installation completed successfully!";
            return true;
        }
        catch (Exception ex)
        {
            Status = $"Installation failed: {ex.Message}";
        }
        return false;
    }

    private bool SetupScheduledTask(string id)
    {
        throw new NotImplementedException(id);
    }

    private bool InstallRemoTerm()
    {
        throw new NotImplementedException();
    }
}