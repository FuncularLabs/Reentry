using Reentry.Core.Abstractions;
using Reentry.Core.Models;

namespace Reentry.Core.Inventory;

/// <summary>
/// Enable/disable a user-scope startup item via StartupApproved. Never deletes the Run value.
/// </summary>
public sealed class StartupToggle
{
    private readonly IRegistryWriter _writer;

    public StartupToggle(IRegistryWriter writer) => _writer = writer;

    public void SetEnabled(StartupInventoryItem item, bool enabled)
    {
        if (!item.IsUserScope)
            throw new InvalidOperationException("Only user-scope startup entries can be toggled from Reentry.");

        var key = StartupApproved.ApprovedKeyFor(item);
        var name = StartupApproved.ApprovedValueName(item);
        var bytes = enabled ? StartupApproved.EnabledBytes : StartupApproved.DisabledBytes;
        _writer.SetBinaryValue(item.Hive, key, name, bytes);
        item.IsEnabled = enabled;
    }
}
