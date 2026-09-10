namespace Reentry.Core.Models;

public enum AppSource
{
    Run,
    RunOnce,
    StartupFolder,
    Task,
    Arr,
    Explorer,
    Managed,
}

public static class AppSourceExtensions
{
    /// <summary>
    /// ARR / Explorer restore rows came from a last-session window or restart registration,
    /// so Interactive still means a visible HWND. Startup inventory (Run, folder, task, …)
    /// includes tray / background apps that never show one.
    /// </summary>
    public static bool ExpectsVisibleWindow(this AppSource source)
        => source is AppSource.Arr or AppSource.Explorer;

    public static bool IsRestoreSource(this AppSource source)
        => source.ExpectsVisibleWindow();
}
