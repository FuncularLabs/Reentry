using Reentry.Core.Models;

namespace Reentry.Core;

/// <summary>
/// Fictitious apps for screenshots / Produce Checklist marketing shots.
/// No real machine names — Contoso-style placeholders only.
/// </summary>
public static class DemoCatalog
{
    public static IReadOnlyList<TrackedApp> Create(DateTimeOffset now)
    {
        // Enough rows that both HUD sections need a vertical scrollbar.
        return
        [
            Row("demo-arr-mail", "Northwind Mail", @"C:\Program Files\Northwind\Mail.exe", AppSource.Arr, AppState.Interactive, now, 42),
            Row("demo-arr-hub", "Adventure Works Hub", @"C:\Program Files\AdventureWorks\Hub.exe", AppSource.Arr, AppState.Interactive, now, 38),
            Row("demo-arr-clip", "Contoso Clipper", @"C:\Program Files\Contoso\Clipper.exe", AppSource.Arr, AppState.Starting, now, 12),
            Row("demo-exp-shell", "Fabrikam Desktop Shell", @"C:\Program Files\Fabrikam\Shell.exe", AppSource.Explorer, AppState.Interactive, now, 55),
            Row("demo-exp-widgets", "Wide World Widgets", @"C:\Program Files\WideWorld\Widgets.exe", AppSource.Explorer, AppState.Pending, now, 8),
            Row("demo-exp-board", "Proseware Whiteboard", @"C:\Program Files\Proseware\Board.exe", AppSource.Explorer, AppState.Hung, now, 96),

            Row("demo-run-sync", "Fabrikam Sync", @"C:\Program Files\Fabrikam\Sync.exe", AppSource.Run, AppState.Interactive, now, 33, managed: true),
            Row("demo-run-notes", "Litware Notes", @"C:\Program Files\Litware\Notes.exe", AppSource.Run, AppState.Interactive, now, 28),
            Row("demo-run-chat", "Proseware Chat", @"C:\Program Files\Proseware\Chat.exe", AppSource.Run, AppState.Starting, now, 15),
            Row("demo-run-photos", "Tailspin Photos", @"C:\Program Files\Tailspin\Photos.exe", AppSource.Run, AppState.Failed, now, 22),
            Row("demo-sf-dock", "Alpine Dock", @"C:\Users\Demo\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\AlpineDock.exe", AppSource.StartupFolder, AppState.Interactive, now, 40),
            Row("demo-sf-coffee", "Fourth Coffee Tray", @"C:\Users\Demo\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\FourthCoffee.exe", AppSource.StartupFolder, AppState.Pending, now, 6),
            Row("demo-task-backup", "Contoso Nightly Backup", @"C:\Program Files\Contoso\Backup.exe", AppSource.Task, AppState.Disabled, now, 0),
            Row("demo-task-update", "Northwind Updater", @"C:\Program Files\Northwind\Update.exe", AppSource.Task, AppState.Interactive, now, 19),
            Row("demo-once-welcome", "Adventure Works Welcome", @"C:\Program Files\AdventureWorks\Welcome.exe", AppSource.RunOnce, AppState.Interactive, now, 11),
            Row("demo-managed-pulse", "Reentry Pulse (demo)", @"C:\Program Files\Funcular\ReentryPulse.exe", AppSource.Managed, AppState.Interactive, now, 25, managed: true),
            Row("demo-run-ledger", "Wide World Ledger", @"C:\Program Files\WideWorld\Ledger.exe", AppSource.Run, AppState.Hung, now, 88),
            Row("demo-run-radar", "Fabrikam Radar", @"C:\Program Files\Fabrikam\Radar.exe", AppSource.Run, AppState.Starting, now, 9),
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