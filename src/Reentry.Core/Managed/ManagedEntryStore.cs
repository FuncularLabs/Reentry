using Reentry.Core.Abstractions;
using Reentry.Core.Inventory;
using Reentry.Core.Models;

namespace Reentry.Core.Managed;

public sealed class ManagedEntryStore
{
    private readonly string _path;
    private List<ManagedEntry> _entries;

    public ManagedEntryStore(string? dataDirectory = null)
    {
        var dir = dataDirectory ?? ReentryPaths.GetDataDirectory();
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, ReentryPaths.ManagedEntriesFileName);
        _entries = JsonUtil.Read<List<ManagedEntry>>(_path) ?? [];
    }

    public string PathOnDisk => _path;

    public IReadOnlyList<ManagedEntry> All => _entries;

    public void Save() => JsonUtil.WriteAtomic(_path, _entries);

    public bool IsManaged(StartupInventoryItem item)
        => _entries.Any(e => Matches(e, item.Name, item.Command, item.ValueName));

    public bool IsManaged(string? name, string? command, string? valueName = null)
        => _entries.Any(e => Matches(e, name, command, valueName));

    public ManagedEntry Add(string name, string command, string? runValueName = null)
    {
        var value = string.IsNullOrWhiteSpace(runValueName) ? name : runValueName;
        var existing = _entries.FirstOrDefault(e =>
            string.Equals(e.RunValueName, value, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            existing.Name = name;
            existing.Command = command;
            Save();
            return existing;
        }

        var entry = new ManagedEntry
        {
            Id = Guid.NewGuid().ToString("n"),
            Name = name,
            Command = command,
            RunValueName = value,
        };
        _entries.Add(entry);
        Save();
        return entry;
    }

    public bool Remove(string nameOrValue)
    {
        var removed = _entries.RemoveAll(e =>
            string.Equals(e.Name, nameOrValue, StringComparison.OrdinalIgnoreCase)
            || string.Equals(e.RunValueName, nameOrValue, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
            Save();
        return removed > 0;
    }

    /// <summary>Write a Reentry-owned HKCU Run entry and record it in the sidecar map.</summary>
    public ManagedEntry AddUserRun(IRegistryWriter writer, string name, string command)
    {
        var valueName = name;
        writer.SetStringValue(
            RegistryHiveKind.CurrentUser,
            StartupRegistryPaths.Run,
            valueName,
            command);
        writer.SetBinaryValue(
            RegistryHiveKind.CurrentUser,
            StartupRegistryPaths.ApprovedRun,
            valueName,
            StartupApproved.EnabledBytes);
        return Add(name, command, valueName);
    }

    /// <summary>Delete a Reentry-owned HKCU Run entry. Leaves other people's values alone.</summary>
    public bool RemoveUserRun(IRegistryWriter writer, string nameOrValue)
    {
        var match = _entries.FirstOrDefault(e =>
            string.Equals(e.Name, nameOrValue, StringComparison.OrdinalIgnoreCase)
            || string.Equals(e.RunValueName, nameOrValue, StringComparison.OrdinalIgnoreCase));
        if (match is null)
            return false;

        writer.DeleteValue(RegistryHiveKind.CurrentUser, StartupRegistryPaths.Run, match.RunValueName);
        writer.DeleteValue(RegistryHiveKind.CurrentUser, StartupRegistryPaths.ApprovedRun, match.RunValueName);
        return Remove(match.RunValueName);
    }

    private static bool Matches(ManagedEntry entry, string? name, string? command, string? valueName)
    {
        if (!string.IsNullOrWhiteSpace(valueName)
            && string.Equals(entry.RunValueName, valueName, StringComparison.OrdinalIgnoreCase))
            return true;

        if (!string.IsNullOrWhiteSpace(name)
            && (string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(entry.RunValueName, name, StringComparison.OrdinalIgnoreCase)))
            return true;

        return CommandText.SameExecutable(entry.Command, command);
    }
}
