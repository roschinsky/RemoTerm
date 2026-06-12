using System;
using System.Diagnostics;
using TRoschinsky.Common;

namespace RemoTerm;

public class ActionProcessor
{
    private ActionConfig config { get; set; }
    public List<JournalEntry> Log { get; set; } = [];

    public ActionProcessor(ActionConfig config)
    {
        this.config = config;
        ProcessAction();
    }

    private void ProcessAction()
    {
        switch(config.ActionType)
        {
            case ActionType.Check:
            Check();
            break;

            case ActionType.Terminate:
            Terminate();
            break;

            case ActionType.Execute:
            Execute();
            break;

            case ActionType.Message:
            Message();
            break;

            case ActionType.Unknown:
            default:
            break;
        }
    }

    private void Message()
    {
        try
        {
            string[] parts = config.Payload.Split('|');
            string title = parts.Length > 1 ? parts[0] : "RemoTerm Message";
            string message = parts.Length > 1 ? parts[1] : config.Payload;
            MessageBoxIcon messageType = parts.Length > 2 ? Enum.Parse<MessageBoxIcon>(parts[2]) : MessageBoxIcon.Information;
            MessageBox.Show(message, title, MessageBoxButtons.OK, messageType);
        }
        catch (Exception ex)
        {
            Log.Add(new JournalEntry($"A message-action failed: {ex.Message}", ex));
        }
    }

    private void Check()
    {
        try
        {
            throw new NotImplementedException("Check action is not implemented yet.");
        }
        catch (Exception ex)
        {
            Log.Add(new JournalEntry($"A check-action failed: {ex.Message}", ex));
        }
    }

    private void Terminate()
    {
        try
        {
            Process[] procs = [.. Process.GetProcesses().Where(p => p.ProcessName.Contains(config.Payload, StringComparison.OrdinalIgnoreCase))];
            if (procs.Length == 0)
            {
                Log.Add(new JournalEntry($"No processes containing '{config.Payload}' found."));
                return;
            }

            Log.Add(new JournalEntry($"Found {procs.Length} processes."));

            foreach (Process proc in procs)
            {
                Log.Add(new JournalEntry($"Terminating process {proc.ProcessName} ('{proc.MainWindowTitle}') with PID {proc.Id} and {proc.Modules.Count} modules."));
                if (!proc.CloseMainWindow())
                {
                    Log.Add(new JournalEntry($"Killing process {proc.ProcessName}."));
                    proc.Kill();
                }
                else
                {
                    Log.Add(new JournalEntry($"Successfully sent close signal to process {proc.ProcessName}."));
                }
            }
        }
        catch (Exception ex)
        {
            Log.Add(new JournalEntry($"Termination failed for process {config.Payload}.", ex));
        }
    }

    private void Execute()
    {
        try
        {
            Process.Start(config.Payload);
        }
        catch (Exception ex)
        {
            Log.Add(new JournalEntry($"An execute-action failed: {ex.Message}", ex));
        }
    }
}
