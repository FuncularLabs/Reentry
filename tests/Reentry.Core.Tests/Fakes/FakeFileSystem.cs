using Reentry.Core.Abstractions;

namespace Reentry.Core.Tests.Fakes;

public sealed class FakeFileSystem : IFileSystemProbe
{
    public Dictionary<string, List<string>> Directories { get; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, string> ShortcutTargets { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void AddFile(string directory, string fileName)
    {
        if (!Directories.TryGetValue(directory, out var files))
        {
            files = [];
            Directories[directory] = files;
        }

        var path = directory.TrimEnd('\\', '/') + "/" + fileName;
        if (!files.Contains(path, StringComparer.OrdinalIgnoreCase))
            files.Add(path);
    }

    public bool DirectoryExists(string path) => Directories.ContainsKey(path);

    public IReadOnlyList<string> EnumerateFiles(string directory, string searchPattern = "*")
        => Directories.TryGetValue(directory, out var files) ? files : [];

    public string ResolveShortcutTarget(string path)
        => ShortcutTargets.TryGetValue(path, out var target) ? target : path;
}
