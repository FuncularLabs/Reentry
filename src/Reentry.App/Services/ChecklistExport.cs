using System.Reflection;
using Reentry.App.ViewModels;
using Reentry.Core.Models;

namespace Reentry.App.Services;

internal static class ChecklistExport
{
    public static ChecklistDocument FromHud(HudViewModel vm, DateTimeOffset exportedAt, string? imageFileName)
    {
        ArgumentNullException.ThrowIfNull(vm);
        return new ChecklistDocument
        {
            ExportedAt = exportedAt,
            SessionType = vm.BootBanner,
            SystemLogNote = string.IsNullOrWhiteSpace(vm.BootDetail) ? null : vm.BootDetail,
            SettledCount = vm.SettledCount,
            TotalCount = vm.TotalCount,
            SessionElapsed = vm.FooterElapsed,
            RestoreRows = Map(vm.RestoreRows),
            StartupRows = Map(vm.StartupRows),
            ReentryVersion = ReadVersion(),
            OsBuild = Environment.OSVersion.VersionString,
            MachineName = string.IsNullOrWhiteSpace(Environment.MachineName) ? null : Environment.MachineName,
            ImageFileName = imageFileName,
        };
    }

    private static IReadOnlyList<ChecklistRow> Map(IEnumerable<TrackedAppRow> rows)
        => rows.Select(r => new ChecklistRow
        {
            Name = r.Name,
            Source = r.Source,
            State = r.AppState,
        }).ToList();

    internal static string ReadVersion()
    {
        var asm = typeof(App).Assembly;
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
            return info;
        return asm.GetName().Version?.ToString() ?? "unknown";
    }
}
