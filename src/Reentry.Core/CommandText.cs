namespace Reentry.Core;

/// <summary>
/// Parse Run-key command lines. Path separators are normalized so tests work on Linux.
/// Unquoted "C:\Program Files\..." paths are split at the executable extension, not the first space.
/// </summary>
public static class CommandText
{
    private static readonly string[] ExecutableExtensions =
    [
        ".exe", ".com", ".cmd", ".bat", ".msc", ".scr", ".lnk",
    ];

    public static string ExtractExecutable(string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return string.Empty;

        var text = command.Trim();
        if (text.StartsWith('"'))
        {
            var end = text.IndexOf('"', 1);
            if (end > 1)
                return text[1..end];
        }

        var lower = text.ToLowerInvariant();
        var best = -1;
        var bestLen = 0;
        foreach (var ext in ExecutableExtensions)
        {
            var idx = lower.IndexOf(ext, StringComparison.Ordinal);
            if (idx >= 0 && (best < 0 || idx < best))
            {
                best = idx;
                bestLen = ext.Length;
            }
        }

        if (best >= 0)
            return text[..(best + bestLen)];

        var space = text.IndexOf(' ');
        return space < 0 ? text : text[..space];
    }

    public static string FileName(string? pathOrCommand)
    {
        var exe = ExtractExecutable(pathOrCommand);
        if (string.IsNullOrEmpty(exe))
            return string.Empty;

        var normalized = exe.Replace('\\', '/');
        return Path.GetFileName(normalized);
    }

    public static string FileNameWithoutExtension(string? pathOrCommand)
    {
        var name = FileName(pathOrCommand);
        return string.IsNullOrEmpty(name) ? string.Empty : Path.GetFileNameWithoutExtension(name);
    }

    public static bool SameExecutable(string? left, string? right)
        => string.Equals(
            FileNameWithoutExtension(left),
            FileNameWithoutExtension(right),
            StringComparison.OrdinalIgnoreCase);
}
