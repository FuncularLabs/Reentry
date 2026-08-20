using Reentry.Core.Abstractions;

namespace Reentry.App.Services;

public sealed class Win32FileSystemProbe : IFileSystemProbe
{
    public bool DirectoryExists(string path) => Directory.Exists(path);

    public IReadOnlyList<string> EnumerateFiles(string directory, string searchPattern = "*")
    {
        if (!Directory.Exists(directory))
            return [];
        return Directory.GetFiles(directory, searchPattern);
    }

    public string ResolveShortcutTarget(string path)
    {
        if (!path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            return path;

        try
        {
            var type = Type.GetTypeFromProgID("WScript.Shell");
            if (type is null)
                return path;
            dynamic shell = Activator.CreateInstance(type)!;
            dynamic shortcut = shell.CreateShortcut(path);
            var target = shortcut.TargetPath as string;
            return string.IsNullOrWhiteSpace(target) ? path : target;
        }
        catch
        {
            return path;
        }
    }
}
