using Reentry.Core.Models;

namespace Reentry.Core.Tracking;

public static class TrackedAppSorter
{
    /// <summary>Reentry-managed entries sort first (visual precedence), then name.</summary>
    public static IReadOnlyList<TrackedApp> Sort(IEnumerable<TrackedApp> apps)
        => apps
            .OrderByDescending(a => a.IsManaged)
            .ThenBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
}
