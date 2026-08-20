using Reentry.Core.Abstractions;
using Reentry.Core.Models;

namespace Reentry.Core.Inventory;

public sealed class StartupInventory
{
    private readonly IRegistryReader _registry;
    private readonly IFileSystemProbe _fileSystem;
    private readonly StartupFolderPaths _folders;

    public StartupInventory(
        IRegistryReader registry,
        IFileSystemProbe fileSystem,
        StartupFolderPaths? folders = null)
    {
        _registry = registry;
        _fileSystem = fileSystem;
        _folders = folders ?? StartupFolderPaths.FromEnvironment();
    }

    public IReadOnlyList<StartupInventoryItem> Collect()
    {
        var items = new List<StartupInventoryItem>();

        ReadRun(items, RegistryHiveKind.CurrentUser, StartupRegistryPaths.Run, AppSource.Run, user: true);
        ReadRun(items, RegistryHiveKind.CurrentUser, StartupRegistryPaths.RunOnce, AppSource.RunOnce, user: true);

        // HKLM / Wow64 are best-effort: a null read is a skip, not a throw.
        ReadRun(items, RegistryHiveKind.LocalMachine, StartupRegistryPaths.Run, AppSource.Run, user: false);
        ReadRun(items, RegistryHiveKind.LocalMachine, StartupRegistryPaths.RunOnce, AppSource.RunOnce, user: false);
        ReadRun(items, RegistryHiveKind.LocalMachine, StartupRegistryPaths.Wow64Run, AppSource.Run, user: false);
        ReadRun(items, RegistryHiveKind.LocalMachine, StartupRegistryPaths.Wow64RunOnce, AppSource.RunOnce, user: false);

        ReadFolder(items, _folders.UserStartup, user: true);
        ReadFolder(items, _folders.CommonStartup, user: false);

        ApplyApprovedOverlay(items);
        return items;
    }

    private void ReadRun(
        List<StartupInventoryItem> items,
        RegistryHiveKind hive,
        string key,
        AppSource source,
        bool user)
    {
        var values = _registry.ReadStringValues(hive, key);
        if (values is null)
            return;

        foreach (var (name, command) in values)
        {
            items.Add(new StartupInventoryItem
            {
                Name = name,
                Command = command,
                Source = source,
                Location = key,
                ValueName = name,
                IsEnabled = true,
                IsUserScope = user,
                Hive = hive,
            });
        }
    }

    private void ReadFolder(List<StartupInventoryItem> items, string directory, bool user)
    {
        if (string.IsNullOrWhiteSpace(directory) || !_fileSystem.DirectoryExists(directory))
            return;

        foreach (var path in _fileSystem.EnumerateFiles(directory, "*"))
        {
            var file = CommandText.FileName(path);
            if (file.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase))
                continue;

            var target = _fileSystem.ResolveShortcutTarget(path);
            items.Add(new StartupInventoryItem
            {
                Name = Path.GetFileNameWithoutExtension(file),
                Command = target,
                Source = AppSource.StartupFolder,
                Location = path,
                ValueName = file,
                IsEnabled = true,
                IsUserScope = user,
                Hive = user ? RegistryHiveKind.CurrentUser : RegistryHiveKind.LocalMachine,
            });
        }
    }

    private void ApplyApprovedOverlay(List<StartupInventoryItem> items)
    {
        foreach (var item in items)
        {
            var key = StartupApproved.ApprovedKeyFor(item);
            var valueName = StartupApproved.ApprovedValueName(item);
            var data = _registry.ReadBinaryValue(item.Hive, key, valueName);
            if (data is not null)
                item.IsEnabled = StartupApproved.IsEnabled(data);
        }
    }
}
