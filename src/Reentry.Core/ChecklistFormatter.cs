using System.Globalization;
using System.Text;
using Reentry.Core.Models;

namespace Reentry.Core;

/// <summary>
/// Pure markdown/plain-text builder for Produce Checklist. No I/O, no WinUI.
/// </summary>
public static class ChecklistFormatter
{
    public const string Title = "Reentry checklist";

    public static string SuggestedBaseName(DateTimeOffset timestamp)
        => "reentry-checklist-" + timestamp.ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);

    public static bool IsMarkdownPath(string? path)
        => !string.IsNullOrEmpty(path)
           && Path.GetExtension(path).Equals(".md", StringComparison.OrdinalIgnoreCase);

    public static string? SiblingPngPath(string documentPath)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        if (!IsMarkdownPath(documentPath))
            return null;

        var dir = Path.GetDirectoryName(documentPath);
        var name = Path.GetFileNameWithoutExtension(documentPath);
        if (string.IsNullOrEmpty(name))
            return null;

        var file = name + ".png";
        return string.IsNullOrEmpty(dir) ? file : Path.Combine(dir, file);
    }

    /// <summary>
    /// Honest checkbox: Interactive and Disabled are done-and-ok.
    /// Failed/Hung are settled for the HUD bar but stay unchecked.
    /// </summary>
    public static bool IsCheckedOff(AppState state)
        => state is AppState.Interactive or AppState.Disabled;

    public static string RelativeImageLink(string imageFileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageFileName);
        var name = imageFileName.Replace('\\', '/');
        var slash = name.LastIndexOf('/');
        if (slash >= 0)
            name = name[(slash + 1)..];
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Image file name has no file component.", nameof(imageFileName));
        return "./" + name;
    }

    public static string ToMarkdown(ChecklistDocument document) => Build(document, markdown: true);

    public static string ToPlainText(ChecklistDocument document) => Build(document, markdown: false);

    private static string Build(ChecklistDocument document, bool markdown)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(document.RestoreRows);
        ArgumentNullException.ThrowIfNull(document.StartupRows);

        var exported = FormatTimestamp(document.ExportedAt);
        var sb = new StringBuilder();

        if (markdown)
            sb.Append("# ");
        sb.Append(Title).Append("\n\n");
        sb.Append("Exported: ").Append(exported).Append("\n\n");

        if (markdown)
            sb.Append("**Session:** ");
        else
            sb.Append("Session: ");
        sb.Append(document.SessionType).Append('\n');

        if (!string.IsNullOrWhiteSpace(document.SystemLogNote))
            sb.Append('\n').Append(document.SystemLogNote.Trim()).Append('\n');

        sb.Append('\n');
        if (markdown)
            sb.Append("**Progress:** ");
        else
            sb.Append("Progress: ");
        sb.Append(document.SettledCount.ToString(CultureInfo.InvariantCulture));
        sb.Append(" / ");
        sb.Append(document.TotalCount.ToString(CultureInfo.InvariantCulture));
        sb.Append(" settled · session elapsed ");
        sb.Append(document.SessionElapsed).Append('\n');

        if (markdown && !string.IsNullOrWhiteSpace(document.ImageFileName))
        {
            sb.Append('\n');
            sb.Append("![Reentry HUD](").Append(RelativeImageLink(document.ImageFileName)).Append(")\n");
        }

        AppendSection(sb, markdown, "Session restore", document.RestoreRows);
        AppendSection(sb, markdown, "Startup apps", document.StartupRows);
        AppendAppendix(sb, markdown, document, exported);
        return sb.ToString();
    }

    private static void AppendSection(
        StringBuilder sb,
        bool markdown,
        string title,
        IReadOnlyList<ChecklistRow> rows)
    {
        sb.Append('\n');
        if (markdown)
            sb.Append("## ");
        sb.Append(title).Append("\n\n");

        if (rows.Count == 0)
        {
            sb.Append("(none)\n");
            return;
        }

        foreach (var row in rows)
            sb.Append(FormatRow(row)).Append('\n');
    }

    private static void AppendAppendix(
        StringBuilder sb,
        bool markdown,
        ChecklistDocument document,
        string exported)
    {
        sb.Append('\n');
        if (markdown)
            sb.Append("## ");
        sb.Append("Appendix\n\n");
        sb.Append("- Reentry: ").Append(document.ReentryVersion).Append('\n');
        sb.Append("- OS: ").Append(document.OsBuild).Append('\n');
        sb.Append("- Export time: ").Append(exported).Append('\n');
        if (!string.IsNullOrWhiteSpace(document.MachineName))
            sb.Append("- Machine: ").Append(document.MachineName).Append('\n');
    }

    internal static string FormatRow(ChecklistRow row)
    {
        ArgumentNullException.ThrowIfNull(row);
        var mark = IsCheckedOff(row.State) ? 'x' : ' ';
        return $"- [{mark}] {row.Name} ({row.Source}) — {row.State}";
    }

    internal static string FormatTimestamp(DateTimeOffset timestamp)
        => timestamp.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture);
}
