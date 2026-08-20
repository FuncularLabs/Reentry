namespace Reentry.Core.Models;

public enum AppState
{
    Pending,
    Starting,
    Interactive,
    Failed,
    Hung,
    Disabled,
}

public static class AppStateExtensions
{
    /// <summary>
    /// Terminal for the session progress bar: Interactive, Failed, Hung, Disabled.
    /// Pending and Starting are still unfinished.
    /// </summary>
    public static bool IsSettled(this AppState state) =>
        state is AppState.Interactive or AppState.Failed or AppState.Hung or AppState.Disabled;
}
