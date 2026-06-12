using System;
using System.Diagnostics;
using System.Xml.Linq;
using TRoschinsky.Common;

namespace RemoTerm;

public class Loader : Form
{
    private string configHost = "10.0.27.20";
    private string configId = "17031";
    private bool onlyInLockedMode = false;
    private bool isLockedMode = false;
    private bool isDebug = false;
    private RichTextBox? richtextLog;
    private readonly List<ActionConfig> actions = [];
    private readonly List<JournalEntry> log = [];


    public Loader(string[] args)
    {

#if DEBUG
        isDebug = true;
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
        Printlog();

        if (!isDebug)
        {
            Close();
            Application.Exit();
        }
    }

    private void GetLockedModeStatus()
    {
        try
        {
            Process[] processes = Process.GetProcessesByName("LockApp");
            isLockedMode = processes.Length > 0 && processes[0].Threads[0].ThreadState == System.Diagnostics.ThreadState.Running;
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
            HttpClient client = new HttpClient();
            string url = $"http://{configHost}/addons/xmlapi/sysvar.cgi?ise_id={configId}";
            HttpResponseMessage response = client.GetAsync(url).Result;
            if (response.IsSuccessStatusCode)
            {
                string xml = response.Content.ReadAsStringAsync().Result;
                log.Add(new JournalEntry($"Received config: {xml}"));

                XDocument doc = XDocument.Parse(xml);
                if (doc != null && doc.Descendants("systemVariable").FirstOrDefault() != null)
                {
                    string configValue = doc.Descendants("systemVariable").First().Attribute("value")?.Value ?? String.Empty;
                    log.Add(new JournalEntry($"Parsed config value: {configValue}"));

                    if (!string.IsNullOrEmpty(configValue))
                    {
                        string[] actionConfigValues = configValue.Split(';', StringSplitOptions.RemoveEmptyEntries);
                        foreach (string action in actionConfigValues)
                        {
                            ActionConfig actionConfig = new()
                            {
                                ActionType = ActionType.Terminate,
                                Title = "Terminate Process",
                                Payload = action.Trim()
                            };
                            actions.Add(actionConfig);
                        }
                    }
                    else
                    {
                        log.Add(new JournalEntry("Config value is empty."));
                    }
                }
            }
            else
            {
                log.Add(new JournalEntry($"Failed to retrieve config. Status code: {response.StatusCode}", true));
            }
        }
        catch (Exception ex)
        {
            log.Add(new JournalEntry($"An error occurred while retrieving config: {ex.Message}", ex));
        }
    }

    private void ProcessActions()
    {
        if (onlyInLockedMode && !isLockedMode)
        {
            log.Add(new JournalEntry("Skipping actions: not in locked mode."));
            return;
        }

        foreach (ActionConfig action in actions)
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

    private void Printlog()
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
