namespace Reentry.Core.Models;

public sealed class StartupInventoryItem
{
    public required string Name { get; set; }
    public required string Command { get; set; }
    public AppSource Source { get; set; }
    public string? Location { get; set; }
    public string? ValueName { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsUserScope { get; set; }
    public RegistryHiveKind Hive { get; set; } = RegistryHiveKind.CurrentUser;
}
