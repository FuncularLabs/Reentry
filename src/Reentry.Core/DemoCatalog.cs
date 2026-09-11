using Reentry.Core.Models;

namespace Reentry.Core;

/// <summary>
/// Mixed fictitious Contoso-style apps plus familiar Windows / everyday names
/// for screenshots. Safe to capture — not read from the live machine.
/// </summary>
public static class DemoCatalog
{
    public static IReadOnlyList<TrackedApp> Create(DateTimeOffset now)
    {
        // Enough rows that both HUD sections need a vertical scrollbar.
        return
        [
            // Session restore (Arr / Explorer) — familiar desktop surface
            Row("demo-arr-outlook", "Outlook", @"C:\Program Files\Microsoft Office\root\Office16\OUTLOOK.EXE", AppSource.Arr, AppState.Interactive, now, 48),
            Row("demo-arr-teams", "Microsoft Teams", @"C:\Users\Demo\AppData\Local\Microsoft\Teams\current\Teams.exe", AppSource.Arr, AppState.Starting, now, 22),
            Row("demo-arr-slack", "Slack", @"C:\Users\Demo\AppData\Local\slack\slack.exe", AppSource.Arr, AppState.Interactive, now, 41),
            Row("demo-arr-chrome", "Google Chrome", @"C:\Program Files\Google\Chrome\Application\chrome.exe", AppSource.Arr, AppState.Interactive, now, 55),
            Row("demo-exp-explorer", "File Explorer", @"C:\Windows\explorer.exe", AppSource.Explorer, AppState.Interactive, now, 60),
            Row("demo-exp-dwm", "Desktop Window Manager", @"C:\Windows\System32\dwm.exe", AppSource.Explorer, AppState.Hung, now, 120),
            Row("demo-exp-widgets", "Windows Widgets", @"C:\Program Files\WindowsApps\MicrosoftWindows.Client.WebExperience\WidgetBoard.exe", AppSource.Explorer, AppState.Pending, now, 9),
            Row("demo-arr-onedrive", "OneDrive", @"C:\Users\Demo\AppData\Local\Microsoft\OneDrive\OneDrive.exe", AppSource.Arr, AppState.Interactive, now, 37),
            Row("demo-arr-clip", "Contoso Clipper", @"C:\Program Files\Contoso\Clipper.exe", AppSource.Arr, AppState.Starting, now, 12),

            // Startup — mix of real annoyances + a few Contoso fillers
            Row("demo-run-outlook", "Microsoft Outlook", @"C:\Program Files\Microsoft Office\root\Office16\OUTLOOK.EXE", AppSource.Run, AppState.Interactive, now, 33, managed: true),
            Row("demo-run-teams", "Teams", @"C:\Users\Demo\AppData\Local\Microsoft\Teams\Update.exe", AppSource.Run, AppState.Starting, now, 18),
            Row("demo-run-dropbox", "Dropbox", @"C:\Program Files\Dropbox\Client\Dropbox.exe", AppSource.Run, AppState.Hung, now, 95),
            Row("demo-run-everything", "Everything", @"C:\Program Files\Everything\Everything.exe", AppSource.Run, AppState.Interactive, now, 14),
            Row("demo-run-rasman", "Remote Access Connection Manager", @"C:\Windows\System32\rasman.exe", AppSource.Run, AppState.Hung, now, 88),
            Row("demo-run-search", "Windows Search", @"C:\Windows\SystemApps\Microsoft.Windows.Search\SearchApp.exe", AppSource.Run, AppState.Pending, now, 7),
            Row("demo-sf-ctfmon", "CTF Loader", @"C:\Windows\System32\ctfmon.exe", AppSource.StartupFolder, AppState.Interactive, now, 40),
            Row("demo-sf-security", "Windows Security notification", @"C:\Program Files\Windows Defender\MSASCuiL.exe", AppSource.StartupFolder, AppState.Interactive, now, 29),
            Row("demo-task-update", "Windows Update Medic", @"C:\Windows\System32\WaasMedicAgent.exe", AppSource.Task, AppState.Disabled, now, 0),
            Row("demo-task-runtime", "Runtime Broker", @"C:\Windows\System32\RuntimeBroker.exe", AppSource.Task, AppState.Interactive, now, 19),
            Row("demo-once-setup", "Office First Run", @"C:\Program Files\Microsoft Office\root\Office16\FirstRun.exe", AppSource.RunOnce, AppState.Failed, now, 25),
            Row("demo-managed-pulse", "Reentry Pulse (demo)", @"C:\Program Files\Funcular\ReentryPulse.exe", AppSource.Managed, AppState.Interactive, now, 25, managed: true),
            Row("demo-run-fabrikam", "Fabrikam Sync", @"C:\Program Files\Fabrikam\Sync.exe", AppSource.Run, AppState.Starting, now, 11),
            Row("demo-run-spooler", "Print Spooler", @"C:\Windows\System32\spoolsv.exe", AppSource.Run, AppState.Hung, now, 76),
            Row("demo-run-adobe", "Adobe Creative Cloud", @"C:\Program Files\Adobe\Adobe Creative Cloud\ACC\Creative Cloud.exe", AppSource.Run, AppState.Pending, now, 5),
        ];
    }

    private static TrackedApp Row(
        string id,
        string name,
        string exe,
        AppSource source,
        AppState state,
        DateTimeOffset now,
        int elapsedSeconds,
        bool managed = false)
        => new()
        {
            Id = id,
            Name = name,
            Executable = exe,
            CommandLine = "\"" + exe + "\"",
            Source = source,
            State = state,
            Elapsed = TimeSpan.FromSeconds(elapsedSeconds),
            IsManaged = managed,
            IsEnabled = state != AppState.Disabled,
            FirstExpectedUtc = now - TimeSpan.FromSeconds(elapsedSeconds),
        };
}