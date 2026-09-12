using System.Diagnostics;
using TRoschinsky.Common;

namespace TRoschinsky.RemoTerm;

public class Loader : Form
{
    private string configHost = "localhost:5048";
    private string apiPath = "/api/remoterm";
    private string configId = "1";
    private bool onlyInLockedMode = false;
    private bool isDebug = false;

    private RichTextBox? richtextLog;
    private HttpClient? client;
    private Config? config;
    private readonly List<JournalEntry> log = [];


    public Loader(string[] args)
    {
        try
        {
#if DEBUG
            isDebug = true;
            configId = "42";
#endif

            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    switch (args[i].Trim().ToLower())
                    {
                        case "-h":
                        case "--config-host":
                            configHost = args[i + 1];
                            break;
                        case "-i":
                        case "--config-id":
                            configId = args[i + 1];
                            break;
                        case "-l":
                        case "--operate-locked":
                            onlyInLockedMode = true;
                            break;
                        case "-d":
                        case "--debug":
                            isDebug = true;
                            break;
                    }
                }
            }

            InitializeComponent();
            GetBasicRuntimeInfo();
            bool proceed = GetConfig();

            if (proceed)
            {
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
                log.Add(new JournalEntry($"An error occurred while processing action '{action}': {ex.Message}", ex));
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
            if(isDebug) { log.Add(new JournalEntry($"Locked mode status: {(isLockedMode ? "Locked" : "Unlocked")}")); }
        }
        catch (Exception ex)
        {
            log.Add(new JournalEntry($"An error occurred while checking locked mode status: {ex.Message}", ex));
        }
        return isLockedMode;
    }

    private bool GetConfig()
    {
        bool configRetrieved = false;
        try
        {
            client = new HttpClient();
            string url = $"http://{configHost}{apiPath}/{configId}";
            HttpResponseMessage response = client.GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                config = System.Text.Json.JsonSerializer.Deserialize<Config>(json) ?? new Config();
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
            log.Add(new JournalEntry($"An error occurred while processing config #{configId}: {ex.Message}", ex));
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
            string url = $"http://{configHost}{apiPath}?id={configId}";
            string json = System.Text.Json.JsonSerializer.Serialize(log);
            StringContent content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(url, content).Result;
            if (!response.IsSuccessStatusCode)
            {
                log.Add(new JournalEntry($"Failed to send log. Status code: {response.StatusCode}", true));
            }
        }
        catch (Exception ex)
        {
            log.Add(new JournalEntry($"An error occurred while sending log: {ex.Message}", ex));
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
            log.Add(new JournalEntry($"An error occurred while getting basic runtime info: {ex.Message}", ex));
        }
    }
}
