namespace Reentry.Core.Managed;

/// <summary>
/// Sidecar record of a startup entry Reentry itself wrote. Visual precedence only —
/// we do not claim ownership of the user's other Run keys.
/// </summary>
public sealed class ManagedEntry
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Command { get; set; }
    public string RunValueName { get; set; } = "";
}
