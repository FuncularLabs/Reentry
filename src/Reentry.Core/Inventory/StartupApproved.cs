using Reentry.Core.Models;

namespace Reentry.Core.Inventory;

/// <summary>
/// StartupApproved overlay. A disable writes these bytes; it must not delete the Run value.
/// Layout: 12 bytes, first byte 0x02 = enabled, 0x03 = disabled-by-user.
/// </summary>
public static class StartupApproved
{
    public static readonly byte[] EnabledBytes = [0x02, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];
    public static readonly byte[] DisabledBytes = [0x03, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

    public static bool IsEnabled(byte[]? data)
    {
        if (data is null || data.Length == 0)
            return true;

        return data[0] switch
        {
            0x00 or 0x02 or 0x06 => true,
            _ => false,
        };
    }

    public static string ApprovedKeyFor(StartupInventoryItem item)
    {
        if (item.Source == AppSource.StartupFolder)
            return StartupRegistryPaths.ApprovedStartupFolder;

        if (item.Location is not null
            && item.Location.Contains("WOW6432Node", StringComparison.OrdinalIgnoreCase))
            return StartupRegistryPaths.ApprovedRun32;

        return StartupRegistryPaths.ApprovedRun;
    }

    public static string ApprovedValueName(StartupInventoryItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.ValueName))
            return item.ValueName;

        if (item.Source == AppSource.StartupFolder && !string.IsNullOrWhiteSpace(item.Location))
            return CommandText.FileName(item.Location);

        return item.Name;
    }
}
