using System.Reflection;

namespace Reentry.App;

/// <summary>
/// Titlebar moniker matching Aperture / Markdown Midget:
/// "Reentry v{InformationalVersion}" (falls back to AssemblyVersion / 0.1.0).
/// </summary>
internal static class AppVersion
{
    internal static string Moniker { get; } = BuildMoniker();

    private static string BuildMoniker()
    {
        var asm = typeof(AppVersion).Assembly;
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? asm.GetName().Version?.ToString(3)
            ?? "0.1.0";
        // Strip any "+buildmetadata" NuGet/CI may append.
        var plus = info.IndexOf('+');
        if (plus >= 0)
            info = info[..plus];
        if (!info.StartsWith('v') && !info.StartsWith('V'))
            info = "v" + info;
        return "Reentry " + info;
    }
}