using System;
using System.Diagnostics;
using TRoschinsky.Common;

namespace RemoTerm;

public class Loader : Form
{
    private string configHost = "10.0.27.21:1880";
    private string configId = "1";
    private bool onlyInLockedMode = false;
    private bool isLockedMode = false;
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
            GetLockedModeStatus();
            GetConfig();
            ProcessActions();
            SendLog();

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
        if (onlyInLockedMode && !isLockedMode)
        {
            log.Add(new JournalEntry("Skipping actions: not in locked mode."));
            return;
        }

        if(config == null)
        {
            log.Add(new JournalEntry("Config is null, no actions to process."));
            return;
        }
        
        if(config.Actions.Count == 0)
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

    private void GetLockedModeStatus()
    {
        try
        {
            Process[] processes = Process.GetProcessesByName("LogonUI");
            isLockedMode = processes.Length > 0; // && processes[0].Threads[0].ThreadState == System.Diagnostics.ThreadState.Running;
            log.Add(new JournalEntry($"Locked mode status: {(isLockedMode ? "Locked" : "Unlocked")}"));
        }
        catch (Exception ex)
        {
            log.Add(new JournalEntry($"An error occurred while checking locked mode status: {ex.Message}", ex));
        }
    }

    private void GetConfig()
    {
        try
        {
            client = new HttpClient();
            string url = $"http://{configHost}/api/remoterm?id={configId}";
            HttpResponseMessage response = client.GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                string json = response.Content.ReadAsStringAsync().Result;
                log.Add(new JournalEntry($"Received config #{configId} successfully!"));
                config = System.Text.Json.JsonSerializer.Deserialize<Config>(json) ?? new Config();
                onlyInLockedMode = config.OnlyInLockedMode;
            }
            else
            {
                log.Add(new JournalEntry($"Failed to retrieve config #{configId}. Status code: {response.StatusCode}", true));
            }
        }
        catch (Exception ex)
        {
            log.Add(new JournalEntry($"An error occurred while retrieving config: {ex.Message}", ex));
        }
        finally
        {
            client?.Dispose();
        }
    }

    private void SendLog()
    {
        try
        {
            HttpClient client = new HttpClient();
            string url = $"http://{configHost}/api/remoterm?id={configId}";
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
}
