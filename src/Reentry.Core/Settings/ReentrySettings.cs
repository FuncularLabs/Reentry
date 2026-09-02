namespace Reentry.Core.Settings;

public sealed class ReentrySettings
{
    public bool AutostartConsentGiven { get; set; }
    public bool AutostartEnabled { get; set; }

    /// <summary>Process exited with no window inside this many seconds → Failed.</summary>
    public int FailedExitSeconds { get; set; } = 15;

    /// <summary>
    /// Restore (ARR / Explorer) process still alive with no window after this many seconds → Hung.
    /// Startup inventory that stays alive without a window is Interactive, not Hung.
    /// </summary>
    public int HungNoWindowSeconds { get; set; } = 90;

    /// <summary>Stop promoting Pending/Starting after this many minutes (~10).</summary>
    public int GlobalCapMinutes { get; set; } = 10;

    public int SnapshotIntervalSeconds { get; set; } = 30;
}
