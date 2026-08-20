using Reentry.Core.Inventory;
using Reentry.Core.Managed;
using Reentry.Core.Models;
using Reentry.Core.Tests.Fakes;
using Reentry.Core.Tests.Support;
using Reentry.Core.Tracking;

namespace Reentry.Core.Tests;

public class ManagedSortTests
{
    [Fact]
    public void Sorter_ManagedEntriesFirst_ThenName()
    {
        var rows = new[]
        {
            new TrackedApp { Id = "1", Name = "Zebra", Executable = "z.exe", IsManaged = false },
            new TrackedApp { Id = "2", Name = "Alpha", Executable = "a.exe", IsManaged = false },
            new TrackedApp { Id = "3", Name = "Mango", Executable = "m.exe", IsManaged = true },
            new TrackedApp { Id = "4", Name = "Beta", Executable = "b.exe", IsManaged = true },
        };

        var sorted = TrackedAppSorter.Sort(rows);
        Assert.Equal(["Beta", "Mango", "Alpha", "Zebra"], sorted.Select(a => a.Name).ToArray());
    }

    [Fact]
    public void Tracker_MarksManagedAndSortsFirst()
    {
        using var dir = new TempDir();
        var store = new ManagedEntryStore(dir.Path);
        store.Add("Notes", "C:\\Tools\\notes.exe", "Notes");

        var inventory = new List<StartupInventoryItem>
        {
            new()
            {
                Name = "Steam",
                Command = "C:\\Program Files\\Steam\\steam.exe",
                Source = AppSource.Run,
                ValueName = "Steam",
                IsUserScope = true,
            },
            new()
            {
                Name = "Notes",
                Command = "C:\\Tools\\notes.exe",
                Source = AppSource.Run,
                ValueName = "Notes",
                IsUserScope = true,
            },
        };

        var rows = new StartupTracker().Tick(
            DateTimeOffset.UtcNow,
            new FakeProcessProbe(),
            inventory,
            lastSession: null,
            managed: store.All);

        Assert.Equal(["Notes", "Steam"], rows.Select(r => r.Name).ToArray());
        Assert.True(rows[0].IsManaged);
        Assert.False(rows[1].IsManaged);
    }

    [Fact]
    public void ManagedStore_AddUserRun_WritesSidecarAndRegistry()
    {
        using var dir = new TempDir();
        var registry = new FakeRegistry();
        var store = new ManagedEntryStore(dir.Path);

        store.AddUserRun(registry, "Notes", "C:\\Tools\\notes.exe");

        Assert.True(registry.HasString(RegistryHiveKind.CurrentUser, StartupRegistryPaths.Run, "Notes"));
        Assert.True(StartupApproved.IsEnabled(
            registry.ReadBinaryValue(RegistryHiveKind.CurrentUser, StartupRegistryPaths.ApprovedRun, "Notes")));
        Assert.True(store.IsManaged("Notes", "C:\\Tools\\notes.exe", "Notes"));

        store.RemoveUserRun(registry, "Notes");
        Assert.False(registry.HasString(RegistryHiveKind.CurrentUser, StartupRegistryPaths.Run, "Notes"));
        Assert.Empty(store.All);
    }
}
