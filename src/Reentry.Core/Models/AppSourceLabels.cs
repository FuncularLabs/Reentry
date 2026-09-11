namespace Reentry.Core.Models;

/// <summary>User-facing labels for <see cref="AppSource"/> (HUD chips, checklists).</summary>
public static class AppSourceLabels
{
    public static string Display(AppSource source) => source switch
    {
        AppSource.Arr => "Last session",
        AppSource.Explorer => "Explorer",
        AppSource.Run => "Run",
        AppSource.RunOnce => "Run once",
        AppSource.StartupFolder => "Startup folder",
        AppSource.Task => "Task",
        AppSource.Managed => "Managed",
        _ => source.ToString(),
    };
}