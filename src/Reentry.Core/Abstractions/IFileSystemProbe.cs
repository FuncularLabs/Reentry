namespace Reentry.Core.Abstractions;

public interface IFileSystemProbe
{
    bool DirectoryExists(string path);
    IReadOnlyList<string> EnumerateFiles(string directory, string searchPattern = "*");

    /// <summary>
    /// Resolve a .lnk to its target if possible; otherwise return <paramref name="path"/>.
    /// </summary>
    string ResolveShortcutTarget(string path);
}
