namespace Reentry.Core.Models;

public enum BootKind
{
    /// <summary>User32 1074 — someone asked Windows to shut down or restart.</summary>
    Expected,
    /// <summary>Event 6008 or Kernel-Power 41 — the previous session did not end cleanly.</summary>
    Unexpected,
    /// <summary>No recent restart/crash signal in the System log.</summary>
    Ordinary,
}
