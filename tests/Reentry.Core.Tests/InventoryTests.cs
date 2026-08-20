using Reentry.Core.Inventory;
using Reentry.Core.Models;
using Reentry.Core.Tests.Fakes;

namespace Reentry.Core.Tests;

public class InventoryTests
{
    [Fact]
    public void Collect_MergesRunKeysAndStartupFolder()
    {
        var registry = new FakeRegistry();
        registry.SetString(RegistryHiveKind.CurrentUser, StartupRegistryPaths.Run, "OneDrive",
            "\"C:\\Users\\paul\\AppData\\Local\\Microsoft\\OneDrive\\OneDrive.exe\" /background");
        registry.SetString(RegistryHiveKind.CurrentUser, StartupRegistryPaths.RunOnce, "Setup",
            "C:\\Windows\\Temp\\setup.exe");
        registry.SetString(RegistryHiveKind.LocalMachine, StartupRegistryPaths.Run, "SecurityHealth",
            "C:\\Windows\\system32\\SecurityHealthSystray.exe");
        registry.SetString(RegistryHiveKind.LocalMachine, StartupRegistryPaths.Wow64Run, "Legacy",
            "C:\\Program Files (x86)\\Legacy\\legacy.exe");

        var fs = new FakeFileSystem();
        var userStartup = "C:\\Users\\paul\\AppData\\Roaming\\Microsoft\\Windows\\Start Menu\\Programs\\Startup";
        fs.AddFile(userStartup, "Slack.lnk");
        fs.ShortcutTargets[userStartup + "/Slack.lnk"] = "C:\\Users\\paul\\AppData\\Local\\slack.exe";

        var inventory = new StartupInventory(registry, fs, new StartupFolderPaths
        {
            UserStartup = userStartup,
            CommonStartup = "C:\\ProgramData\\Microsoft\\Windows\\Start Menu\\Programs\\StartUp",
        });

        var items = inventory.Collect();
        Assert.Equal(5, items.Count);
        Assert.Contains(items, i => i.Name == "OneDrive" && i.Source == AppSource.Run && i.IsUserScope);
        Assert.Contains(items, i => i.Name == "Setup" && i.Source == AppSource.RunOnce);
        Assert.Contains(items, i => i.Name == "SecurityHealth" && !i.IsUserScope);
        Assert.Contains(items, i => i.Name == "Legacy" && i.Location == StartupRegistryPaths.Wow64Run);
        Assert.Contains(items, i => i.Name == "Slack" && i.Source == AppSource.StartupFolder && i.IsEnabled);
    }

    [Fact]
    public void Collect_ApprovedDisable_DoesNotDropRunValue()
    {
        var registry = new FakeRegistry();
        registry.SetString(RegistryHiveKind.CurrentUser, StartupRegistryPaths.Run, "Steam",
            "C:\\Program Files\\Steam\\steam.exe -silent");
        registry.SetBinary(
            RegistryHiveKind.CurrentUser,
            StartupRegistryPaths.ApprovedRun,
            "Steam",
            StartupApproved.DisabledBytes);

        var inventory = new StartupInventory(registry, new FakeFileSystem(), new StartupFolderPaths());
        var items = inventory.Collect();

        var steam = Assert.Single(items);
        Assert.Equal("Steam", steam.Name);
        Assert.Contains("steam.exe", steam.Command, StringComparison.OrdinalIgnoreCase);
        Assert.False(steam.IsEnabled);
        Assert.True(registry.HasString(RegistryHiveKind.CurrentUser, StartupRegistryPaths.Run, "Steam"));
    }

    [Fact]
    public void Toggle_WritesApprovedOverlay_LeavesRunValue()
    {
        var registry = new FakeRegistry();
        registry.SetString(RegistryHiveKind.CurrentUser, StartupRegistryPaths.Run, "Steam",
            "C:\\Program Files\\Steam\\steam.exe");

        var inventory = new StartupInventory(registry, new FakeFileSystem(), new StartupFolderPaths());
        var item = Assert.Single(inventory.Collect());
        Assert.True(item.IsEnabled);

        var toggle = new StartupToggle(registry);
        toggle.SetEnabled(item, enabled: false);

        Assert.False(item.IsEnabled);
        Assert.True(registry.HasString(RegistryHiveKind.CurrentUser, StartupRegistryPaths.Run, "Steam"));
        Assert.False(StartupApproved.IsEnabled(
            registry.ReadBinaryValue(RegistryHiveKind.CurrentUser, StartupRegistryPaths.ApprovedRun, "Steam")));

        toggle.SetEnabled(item, enabled: true);
        Assert.True(item.IsEnabled);
        Assert.True(registry.HasString(RegistryHiveKind.CurrentUser, StartupRegistryPaths.Run, "Steam"));
    }

    [Fact]
    public void Collect_HkLmUnreadable_IsSkippedNotThrown()
    {
        var registry = new FakeRegistry { HideLocalMachine = true };
        registry.SetString(RegistryHiveKind.CurrentUser, StartupRegistryPaths.Run, "OnlyUser", "only.exe");
        registry.SetString(RegistryHiveKind.LocalMachine, StartupRegistryPaths.Run, "Machine", "machine.exe");

        var items = new StartupInventory(registry, new FakeFileSystem(), new StartupFolderPaths()).Collect();
        var only = Assert.Single(items);
        Assert.Equal("OnlyUser", only.Name);
    }

    [Fact]
    public void Approved_Wow64MapsToRun32()
    {
        var item = new StartupInventoryItem
        {
            Name = "Legacy",
            Command = "legacy.exe",
            Source = AppSource.Run,
            Location = StartupRegistryPaths.Wow64Run,
            ValueName = "Legacy",
            Hive = RegistryHiveKind.LocalMachine,
        };
        Assert.Equal(StartupRegistryPaths.ApprovedRun32, StartupApproved.ApprovedKeyFor(item));
    }
}
