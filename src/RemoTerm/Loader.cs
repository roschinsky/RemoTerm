using System.Diagnostics;
using System.Text.Json.Serialization;
using TRoschinsky.Common;

namespace TRoschinsky.RemoTerm;

public class Loader : Form
{
    private string configHost = "localhost:5048";
    private string apiPath = "api";
    private string apiToken = string.Empty;
    private string configId = "1";
    private bool onlyInLockedMode = false;
    private bool isDebug = false;
    private bool isOverrideSettings = false;

    private RichTextBox? richtextLog;
    private HttpClient? client;
    private Defaults? defaults;
    private Config? config;
    private Updater? updater;
    private readonly List<JournalEntry> log = [];


    public Loader(string[] args)
    {
        try
        {
#if DEBUG
            configHost = "localhost:5048";
            configId = "42";
            isDebug = true;
            log.Add(new JournalEntry("Debug mode enabled by build configuration."));
#else
            SetDefaults();
#endif

            // Check command line arguments
            bool startInstaller = false;
            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i].Trim().ToLower())
                    {
                        case "-h":
                        case "--config-host":
                            configHost = isOverrideSettings ? configHost : args[i + 1];
                            break;
                        case "-c":
                        case "--config-id":
                            configId = isOverrideSettings ? configId : args[i + 1];
                            break;
                        case "-l":
                        case "--operate-locked":
                            onlyInLockedMode = true;
                            break;
                        case "-d":
                        case "--debug":
                            isDebug = true;
                            log.Add(new JournalEntry("Debug mode enabled by command line."));
                            break;
                        case "-u":
                        case "--update":
                            updater = new Updater();
                            log.Add(new JournalEntry($"Update triggered by command line; status: {updater.Status}"));
                            break;
                        case "-i":
                        case "--install":
                            startInstaller = true;
                            break;
                        case "-t":
                        case "--token":
                            apiToken = args[i + 1];
                            apiToken = apiToken.Trim();
                            break;
                    }
                }
            }

            if (startInstaller)
            {
                Installer installer = new Installer(configId, isDebug);
                Environment.Exit(0);
            }

            InitializeComponent();
            GetBasicRuntimeInfo();
            bool proceed = GetConfig();

            if (proceed)
            {
                // Process update if triggered by command line or config
                if (config != null && config.Update)
                {
                    updater = new Updater(true, config.UpdateInstall);
                    log.Add(new JournalEntry($"Update triggered by config; status: {updater.Status}"));
                }

                // Process actions based on locked mode and delay settings
                if (onlyInLockedMode && !GetLockedModeStatus())
                {
                    log.Add(new JournalEntry("Skipping actions: 1st check - not in locked mode."));
                }
                else if (config?.Delay > TimeSpan.Zero)
                {
                    log.Add(new JournalEntry($"Delaying actions by {config.Delay.TotalSeconds} seconds."));
                    Thread.Sleep(config.Delay);
                    if (onlyInLockedMode && !GetLockedModeStatus())
                    {
                        log.Add(new JournalEntry("Skipping actions: 2nd check - not in locked mode."));
                    }
                    else
                    {
                        ProcessActions();
                    }
                }
                else
                {
                    ProcessActions();
                }
            }
            else
            {
                log.Add(new JournalEntry("Skipping actions: no valid config available."));
            }

            // After processing actions, we want to send the log back to the server for review and debugging.
            SendLog();

            // In debug mode, we want to see the log and keep the application open. Otherwise, we can just exit.
            if (isDebug)
            {
                PrintLog();
                Focus();
                BringToFront();
            }
            else
            {
                Environment.ExitCode = 0;
                Close();
                Application.Exit();
            }
        }
        catch (Exception)
        {
            Environment.ExitCode = 9;
        }
    }

    private void ProcessActions()
    {
        if (config == null)
        {
            log.Add(new JournalEntry("Config is null, no actions to process."));
            return;
        }

        if (config.Actions.Count == 0)
        {
            log.Add(new JournalEntry("Config is empty, no actions to process."));
            return;
        }

        foreach (ActionConfig action in config.Actions)
        {
            try
            {
                log.Add(new JournalEntry($"Processing action: {action}"));
                ActionProcessor actionProcessor = new ActionProcessor(action);
                log.AddRange(actionProcessor.Log);
            }
            catch (Exception ex)
            {
                log.Add(new JournalEntry($"Processing action '{action}' failed: {ex.Message}", ex));
            }
        }
    }

    private bool GetLockedModeStatus()
    {
        bool isLockedMode = false;
        try
        {
            Process[] processes = Process.GetProcessesByName("LogonUI");
            isLockedMode = processes.Length > 0; // && processes[0].Threads[0].ThreadState == System.Diagnostics.ThreadState.Running;
            if (isDebug) { log.Add(new JournalEntry($"Locked mode status: {(isLockedMode ? "Locked" : "Unlocked")}")); }
        }
        catch (Exception ex)
        {
            log.Add(new JournalEntry($"Checking locked mode status failed: {ex.Message}", ex));
        }
        return isLockedMode;
    }

    private bool GetConfig()
    {
        bool configRetrieved = false;
        try
        {
            client = new HttpClient();
            string url = $"http://{configHost}/{apiPath}/configs/{configId}";
            HttpResponseMessage response = client.GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                config = System.Text.Json.JsonSerializer.Deserialize<Config>(json);
                if (config == null)
                {
                    log.Add(new JournalEntry($"Failed to deserialize config data #{configId}.", true));
                    return false;
                }
                onlyInLockedMode = config.OnlyInLockedMode;
                log.Add(new JournalEntry($"Retrieved config data #{configId} successfully!"));
                configRetrieved = true;
            }
            else
            {
                log.Add(new JournalEntry($"Failed to retrieve config data #{configId} with status: {response.StatusCode}", true));
            }
        }
        catch (Exception ex)
        {
            log.Add(new JournalEntry($"Processing config #{configId} failed: {ex.Message}", ex));
        }
        finally
        {
            client?.Dispose();
        }
        return configRetrieved;
    }

    private void SendLog()
    {
        try
        {
            HttpClient client = new HttpClient();
            string url = $"http://{configHost}/{apiPath}/logs/{configId}";
            string json = System.Text.Json.JsonSerializer.Serialize(LogEntriesFromJournalEntries(log));
            if (!string.IsNullOrEmpty(apiToken))
            {
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);
            }
            StringContent content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(url, content).Result;
            if (!response.IsSuccessStatusCode)
            {
                log.Add(new JournalEntry($"Sending log failed with status code: {response.StatusCode}", true));
            }
        }
        catch (Exception ex)
        {
            log.Add(new JournalEntry($"Sending log failed: {ex.Message}", ex));
        }
        finally
        {
            client?.Dispose();
        }
    }

    private void PrintLog()
    {
        if (richtextLog != null)
        {
            foreach (JournalEntry entry in log)
            {
                richtextLog.AppendText($"{entry}{Environment.NewLine}");
            }
            richtextLog.ScrollToCaret();
        }
    }

    private void InitializeComponent()
    {
        Text = "RemoTerm Loader";
        Size = new Size(600, 400);
        MinimizeBox = true;
        ControlBox = true;
        ResumeLayout(true);

        if (!isDebug)
        {
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            //this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
        }

        richtextLog = new RichTextBox()
        {
            Left = 10,
            Top = 10,
            Width = 550,
            Height = 310,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            ReadOnly = true,
            WordWrap = false,
            ScrollBars = RichTextBoxScrollBars.Both,
            BackColor = SystemColors.Window,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Consolas", 8)
        };

        this.Controls.Add(richtextLog);

        ResumeLayout(false);
        PerformLayout();
    }

    private void GetBasicRuntimeInfo()
    {
        try
        {
            log.Add(new JournalEntry($"Running on machine '{Environment.MachineName}' as {Environment.UserName}."));
            log.Add(new JournalEntry($"Current directory: {Environment.CurrentDirectory}"));
        }
        catch (Exception ex)
        {
            log.Add(new JournalEntry($"Getting basic runtime info failed: {ex.Message}", ex));
        }
    }

    private void SetDefaults()
    {
        try
        {
            defaults = new(this.GetType().Namespace, typeof(DefaultValues));
            if (defaults.Success)
            {
                DefaultValues? init = defaults.DefaultValues as DefaultValues;
                if (init != null)
                {
                    configHost = string.IsNullOrWhiteSpace(init.ConfigHost) ? configHost : init.ConfigHost;
                    apiPath = string.IsNullOrWhiteSpace(init.ApiPath) ? apiPath : init.ApiPath.Trim().Trim('/');
                    configId = string.IsNullOrWhiteSpace(init.ConfigId) ? configId : init.ConfigId;
                    isOverrideSettings = init.OverrideSettings;
                    log.Add(new JournalEntry("Default values initialized."));
                }
                if (isOverrideSettings)
                {
                    log.Add(new JournalEntry("Overriding manual parameters."));
                }
            }
            else
            {
                log.Add(new JournalEntry(defaults.Status, true));
            }
        }
        catch (Exception ex)
        {
            log.Add(new JournalEntry($"Setting default values failed: {ex.Message}", ex));
        }
    }


    private static LogEntry[] LogEntriesFromJournalEntries(List<JournalEntry> journalEntries)
    {
        try
        {
            if (journalEntries == null || journalEntries.Count == 0)
            {
                return [];
            }
            else
            {
                return journalEntries.Select(j => new LogEntry(j.IsError ? "ERR" : j.IsWarning ? "WRN" : "INF", j.Message, j.Error, j.TimeStamp)).ToArray();
            }
        }
        catch (Exception ex)
        {
            return [new LogEntry("ERR", $"Processing journal entries failed: {ex.Message}", ex)];
        }
    }
}

public record DefaultValues
{
    [JsonPropertyName("configHost")]
    public string? ConfigHost { get; init; }
    [JsonPropertyName("configId")]
    public string? ConfigId { get; init; }
    [JsonPropertyName("override")]
    public bool OverrideSettings { get; init; } = false;

    [JsonPropertyName("apiPath")]
    public string? ApiPath { get; init; }

};