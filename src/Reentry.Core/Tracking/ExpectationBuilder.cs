using Reentry.Core.Managed;
using Reentry.Core.Models;

namespace Reentry.Core.Tracking;

public static class ExpectationBuilder
{
    public static IReadOnlyList<TrackedApp> Build(
        SessionSnapshot? lastSession,
        IReadOnlyList<StartupInventoryItem> inventory,
        IReadOnlyCollection<ManagedEntry> managed)
    {
        var apps = new List<TrackedApp>();
        var seenExe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in inventory)
        {
            var exe = CommandText.ExtractExecutable(item.Command);
            var app = new TrackedApp
            {
                Id = $"inv:{item.Source}:{item.ValueName ?? item.Name}:{item.Location}",
                Name = item.Name,
                Executable = exe,
                CommandLine = item.Command,
                Source = item.Source,
                State = item.IsEnabled ? AppState.Pending : AppState.Disabled,
                IsEnabled = item.IsEnabled,
                IsManaged = managed.Any(m =>
                    string.Equals(m.RunValueName, item.ValueName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(m.Name, item.Name, StringComparison.OrdinalIgnoreCase)
                    || CommandText.SameExecutable(m.Command, item.Command)),
                Location = item.Location,
                ValueName = item.ValueName,
                IsUserScope = item.IsUserScope,
            };
            if (app.IsManaged && app.Source != AppSource.Managed)
            {
                // Source stays the real one (Run / folder); the badge is IsManaged.
            }

            apps.Add(app);
            var key = CommandText.FileNameWithoutExtension(exe);
            if (!string.IsNullOrEmpty(key))
                seenExe.Add(key);
        }

        if (lastSession is not null)
        {
            foreach (var process in lastSession.Processes.Where(p => p.IsArrRegistered))
            {
                var key = CommandText.FileNameWithoutExtension(process.Executable);
                if (string.IsNullOrEmpty(key) || !seenExe.Add(key))
                    continue;

                apps.Add(new TrackedApp
                {
                    Id = $"arr:{key}",
                    Name = key,
                    Executable = process.Executable,
                    CommandLine = process.CommandLine,
                    Source = AppSource.Arr,
                    State = AppState.Pending,
                    IsEnabled = true,
                    IsManaged = false,
                });
            }

            foreach (var window in lastSession.Windows.Where(w => w.IsVisible))
            {
                var key = CommandText.FileNameWithoutExtension(window.Executable);
                if (string.IsNullOrEmpty(key) || !seenExe.Add(key))
                    continue;

                apps.Add(new TrackedApp
                {
                    Id = $"explorer:{key}",
                    Name = string.IsNullOrWhiteSpace(window.Title) ? key : window.Title,
                    Executable = window.Executable,
                    Source = AppSource.Explorer,
                    State = AppState.Pending,
                    IsEnabled = true,
                    IsManaged = false,
                });
            }
        }

        return apps;
    }
}
