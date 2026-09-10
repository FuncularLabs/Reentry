using System.Globalization;
using Reentry.Core;
using Reentry.Core.Models;
using Reentry.Core.Tests.Support;

namespace Reentry.Core.Tests;

public class ChecklistFormatterTests
{
    private static readonly DateTimeOffset ExportedAt =
        new(2026, 9, 10, 14, 5, 7, TimeSpan.FromHours(-4));

    [Fact]
    public void SuggestedBaseName_UsesLocalClock_NotUtc_ZeroPadded()
    {
        // 00:30 local is 04:30 UTC — UTC formatting would write 0430.
        var local = new DateTimeOffset(2026, 9, 10, 0, 30, 59, TimeSpan.FromHours(-4));
        Assert.Equal("reentry-checklist-20260910-0030", ChecklistFormatter.SuggestedBaseName(local));
    }

    [Fact]
    public void SuggestedBaseName_DoesNotUseUtcDayRollover()
    {
        // 23:15 +02:00 is 21:15 UTC the same day; a UTC converter that adds
        // offset the wrong way could land on 2026-09-11.
        var local = new DateTimeOffset(2026, 9, 10, 23, 15, 0, TimeSpan.FromHours(2));
        Assert.Equal("reentry-checklist-20260910-2315", ChecklistFormatter.SuggestedBaseName(local));
    }

    [Fact]
    public void SuggestedBaseName_OmitsSeconds()
    {
        var local = new DateTimeOffset(2026, 1, 2, 9, 5, 59, TimeSpan.Zero);
        var name = ChecklistFormatter.SuggestedBaseName(local);
        Assert.Equal("reentry-checklist-20260102-0905", name);
        Assert.DoesNotContain("59", name);
    }

    [Fact]
    public void SuggestedBaseName_IsCultureInvariant()
    {
        var original = CultureInfo.CurrentCulture;
        var originalUi = CultureInfo.CurrentUICulture;
        try
        {
            var de = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentCulture = de;
            CultureInfo.CurrentUICulture = de;
            var local = new DateTimeOffset(2026, 9, 10, 14, 5, 7, TimeSpan.FromHours(2));
            Assert.Equal("reentry-checklist-20260910-1405", ChecklistFormatter.SuggestedBaseName(local));
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.CurrentUICulture = originalUi;
        }
    }

    [Theory]
    [InlineData("notes.md", true)]
    [InlineData("notes.MD", true)]
    [InlineData("notes.txt", false)]
    [InlineData("notes.markdown", false)]
    [InlineData("notes.md.txt", false)]
    [InlineData("", false)]
    public void IsMarkdownPath_OnlyDotMd(string path, bool expected)
        => Assert.Equal(expected, ChecklistFormatter.IsMarkdownPath(path));

    [Fact]
    public void SiblingPngPath_Markdown_SameDirectoryAndBasename()
    {
        using var dir = new TempDir();
        var md = Path.Combine(dir.Path, "reentry-checklist-20260910-1405.md");
        Assert.Equal(
            Path.Combine(dir.Path, "reentry-checklist-20260910-1405.png"),
            ChecklistFormatter.SiblingPngPath(md));
    }

    [Fact]
    public void SiblingPngPath_PlainText_IsNull()
    {
        using var dir = new TempDir();
        var txt = Path.Combine(dir.Path, "reentry-checklist-20260910-1405.txt");
        Assert.Null(ChecklistFormatter.SiblingPngPath(txt));
    }

    [Fact]
    public void SiblingPngPath_Markdown_UserRenamedFile_FollowsActualBasename()
    {
        using var dir = new TempDir();
        var md = Path.Combine(dir.Path, "my-boot.md");
        Assert.Equal(Path.Combine(dir.Path, "my-boot.png"), ChecklistFormatter.SiblingPngPath(md));
    }

    [Theory]
    [InlineData(AppState.Interactive, true)]
    [InlineData(AppState.Disabled, true)]
    [InlineData(AppState.Pending, false)]
    [InlineData(AppState.Starting, false)]
    [InlineData(AppState.Failed, false)]
    [InlineData(AppState.Hung, false)]
    public void IsCheckedOff_InteractiveAndDisabledOnly(AppState state, bool expected)
        => Assert.Equal(expected, ChecklistFormatter.IsCheckedOff(state));

    [Fact]
    public void IsCheckedOff_IsNotIsSettled()
    {
        // Mutation this kills: `return state.IsSettled()` — Failed/Hung are
        // settled for the progress bar but must not render as [x].
        Assert.True(AppState.Failed.IsSettled());
        Assert.True(AppState.Hung.IsSettled());
        Assert.False(ChecklistFormatter.IsCheckedOff(AppState.Failed));
        Assert.False(ChecklistFormatter.IsCheckedOff(AppState.Hung));
    }

    [Fact]
    public void IsCheckedOff_CoversEveryEnumValue()
    {
        foreach (var state in Enum.GetValues<AppState>())
        {
            var expected = state is AppState.Interactive or AppState.Disabled;
            Assert.Equal(expected, ChecklistFormatter.IsCheckedOff(state));
        }
    }

    [Fact]
    public void ToMarkdown_TitleTimestampSessionProgressSectionsAppendix()
    {
        var md = ChecklistFormatter.ToMarkdown(Sample());

        Assert.StartsWith("# Reentry checklist", md, StringComparison.Ordinal);
        Assert.Contains("Exported: 2026-09-10 14:05:07 -04:00", md);
        Assert.Contains("**Session:** Ordinary logon", md);
        Assert.Contains("No recent planned or dirty shutdown in the System log.", md);
        Assert.Contains("**Progress:** 2 / 3 settled · session elapsed 04:21", md);
        Assert.Contains("## Session restore", md);
        Assert.Contains("## Startup apps", md);
        Assert.Contains("## Appendix", md);
        Assert.Contains("- Reentry: 0.1.0-alpha1", md);
        Assert.Contains("- OS: Microsoft Windows NT 10.0.26200.0", md);
        Assert.Contains("- Export time: 2026-09-10 14:05:07 -04:00", md);
        Assert.Contains("- Machine: FUNCULAR-WKS-02", md);
    }

    [Fact]
    public void ToMarkdown_OmitsSystemLogNote_WhenMissing()
    {
        var md = ChecklistFormatter.ToMarkdown(Sample() with { SystemLogNote = null });
        Assert.DoesNotContain("No recent planned", md);
        Assert.Contains("**Session:** Ordinary logon", md);

        var whitespace = ChecklistFormatter.ToMarkdown(Sample() with { SystemLogNote = "   " });
        Assert.DoesNotContain("**System", whitespace);
    }

    [Fact]
    public void ToMarkdown_OmitsMachine_WhenMissing()
    {
        var md = ChecklistFormatter.ToMarkdown(Sample() with { MachineName = null });
        Assert.DoesNotContain("Machine:", md);
    }

    [Fact]
    public void ToMarkdown_RelativeImageLink_WhenImageFileNamePresent()
    {
        var md = ChecklistFormatter.ToMarkdown(Sample() with
        {
            ImageFileName = "reentry-checklist-20260910-1405.png",
        });
        Assert.Contains("![Reentry HUD](./reentry-checklist-20260910-1405.png)", md);
    }

    [Fact]
    public void ToMarkdown_ImageLink_UsesFileNameOnly_EvenIfFullPathGiven()
    {
        // Linux CI: Path.GetFileName(@"C:\temp\a.png") does not strip directories.
        var md = ChecklistFormatter.ToMarkdown(Sample() with
        {
            ImageFileName = @"C:\temp\reentry-checklist-20260910-1405.png",
        });
        Assert.Contains("![Reentry HUD](./reentry-checklist-20260910-1405.png)", md);
        Assert.DoesNotContain(@"C:\temp", md);
        Assert.DoesNotContain("C:/temp", md);
    }

    [Fact]
    public void ToMarkdown_OmitsImageLink_WhenNoImageFileName()
    {
        var md = ChecklistFormatter.ToMarkdown(Sample() with { ImageFileName = null });
        Assert.DoesNotContain("![", md);
        Assert.DoesNotContain(".png", md);
    }

    [Fact]
    public void ToMarkdown_RestoreAndStartupRows_HonestCheckmarksAndStatus()
    {
        var md = ChecklistFormatter.ToMarkdown(Sample());
        Assert.Contains("- [x] Outlook (Arr) — Interactive", md);
        Assert.Contains("- [ ] Chrome (Arr) — Pending", md);
        Assert.Contains("- [x] OneDrive (Run) — Disabled", md);
        Assert.Contains("- [ ] Dropbox (Run) — Hung", md);
        Assert.Contains("- [ ] Steam (Run) — Failed", md);
        Assert.Contains("- [ ] Slack (Run) — Starting", md);
    }

    [Fact]
    public void ToMarkdown_DoesNotMixSections()
    {
        var md = ChecklistFormatter.ToMarkdown(Sample());
        var restoreAt = md.IndexOf("## Session restore", StringComparison.Ordinal);
        var startupAt = md.IndexOf("## Startup apps", StringComparison.Ordinal);
        var appendixAt = md.IndexOf("## Appendix", StringComparison.Ordinal);
        Assert.True(restoreAt >= 0 && startupAt > restoreAt && appendixAt > startupAt);

        var restore = md[restoreAt..startupAt];
        var startup = md[startupAt..appendixAt];
        Assert.Contains("Outlook", restore);
        Assert.Contains("Chrome", restore);
        Assert.DoesNotContain("OneDrive", restore);
        Assert.Contains("OneDrive", startup);
        Assert.DoesNotContain("Outlook", startup);
    }

    [Fact]
    public void ToMarkdown_EmptySections_WriteNone()
    {
        var md = ChecklistFormatter.ToMarkdown(Sample() with
        {
            RestoreRows = [],
            StartupRows = [],
            SettledCount = 0,
            TotalCount = 0,
        });
        Assert.Contains("## Session restore", md);
        Assert.Contains("## Startup apps", md);
        Assert.Contains("(none)", md);
        Assert.DoesNotContain("- [", md);
    }

    [Fact]
    public void ToMarkdown_ExportsDisplayNamesAsGiven_NoRedaction()
    {
        var doc = Sample() with
        {
            RestoreRows =
            [
                new ChecklistRow { Name = "Paul's Outlook", Source = "Arr", State = AppState.Interactive },
            ],
            StartupRows = [],
        };
        var md = ChecklistFormatter.ToMarkdown(doc);
        Assert.Contains("Paul's Outlook", md);
        Assert.DoesNotContain("redact", md, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("***", md);
    }

    [Fact]
    public void ToMarkdown_DoesNotInventExecutableOrCommandLine()
    {
        var md = ChecklistFormatter.ToMarkdown(Sample());
        Assert.DoesNotContain(".exe", md, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"C:\", md);
        Assert.DoesNotContain("CommandLine", md);
        Assert.DoesNotContain("Executable", md);
    }

    [Fact]
    public void ToMarkdown_PreservesRowOrder()
    {
        var doc = Sample() with
        {
            RestoreRows =
            [
                new ChecklistRow { Name = "Zed", Source = "Arr", State = AppState.Interactive },
                new ChecklistRow { Name = "Ann", Source = "Explorer", State = AppState.Pending },
            ],
            StartupRows = [],
        };
        var md = ChecklistFormatter.ToMarkdown(doc);
        Assert.True(md.IndexOf("Zed", StringComparison.Ordinal) < md.IndexOf("Ann", StringComparison.Ordinal));
    }

    [Fact]
    public void ToMarkdown_FormatsAreCultureInvariant()
    {
        var original = CultureInfo.CurrentCulture;
        var originalUi = CultureInfo.CurrentUICulture;
        try
        {
            var de = CultureInfo.GetCultureInfo("de-DE");
            CultureInfo.CurrentCulture = de;
            CultureInfo.CurrentUICulture = de;
            var md = ChecklistFormatter.ToMarkdown(Sample());
            Assert.Contains("2026-09-10 14:05:07 -04:00", md);
            Assert.Contains("2 / 3 settled", md);
            Assert.DoesNotContain("10.09.2026", md);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
            CultureInfo.CurrentUICulture = originalUi;
        }
    }

    [Fact]
    public void ToPlainText_SameRows_NoMarkdownImageOrHashes()
    {
        var txt = ChecklistFormatter.ToPlainText(Sample() with
        {
            ImageFileName = "reentry-checklist-20260910-1405.png",
        });
        Assert.DoesNotContain("![", txt);
        Assert.DoesNotContain(".png", txt);
        Assert.DoesNotContain("# Reentry", txt);
        Assert.DoesNotContain("**Session:**", txt);
        Assert.DoesNotContain("## ", txt);
        Assert.StartsWith("Reentry checklist", txt, StringComparison.Ordinal);
        Assert.Contains("Session: Ordinary logon", txt);
        Assert.Contains("Progress: 2 / 3 settled · session elapsed 04:21", txt);
        Assert.Contains("- [x] Outlook (Arr) — Interactive", txt);
        Assert.Contains("- [ ] Chrome (Arr) — Pending", txt);
        Assert.Contains("- [x] OneDrive (Run) — Disabled", txt);
        Assert.Contains("Session restore", txt);
        Assert.Contains("Startup apps", txt);
        Assert.Contains("Appendix", txt);
        Assert.Contains("Machine: FUNCULAR-WKS-02", txt);
    }

    [Fact]
    public void ToPlainText_OmitsImageAndKeepsSystemLog()
    {
        var txt = ChecklistFormatter.ToPlainText(Sample());
        Assert.Contains("No recent planned or dirty shutdown in the System log.", txt);
        Assert.DoesNotContain("![", txt);
    }

    [Fact]
    public void RelativeImageLink_StripsUnixAndWindowsDirectories()
    {
        Assert.Equal("./shot.png", ChecklistFormatter.RelativeImageLink("shot.png"));
        Assert.Equal("./shot.png", ChecklistFormatter.RelativeImageLink("/tmp/shot.png"));
        Assert.Equal("./shot.png", ChecklistFormatter.RelativeImageLink(@"C:\temp\shot.png"));
        Assert.Equal("./shot.png", ChecklistFormatter.RelativeImageLink(@"C:/temp/shot.png"));
    }

    [Fact]
    public void ToMarkdown_ThrowsOnNullDocument()
        => Assert.Throws<ArgumentNullException>(() => ChecklistFormatter.ToMarkdown(null!));

    [Fact]
    public void ToPlainText_ThrowsOnNullDocument()
        => Assert.Throws<ArgumentNullException>(() => ChecklistFormatter.ToPlainText(null!));

    [Fact]
    public void SuggestedBaseName_MinValueStillFormats()
    {
        var name = ChecklistFormatter.SuggestedBaseName(DateTimeOffset.MinValue);
        Assert.StartsWith("reentry-checklist-", name, StringComparison.Ordinal);
    }

    [Fact]
    public void IsMarkdownPath_Null_IsFalse()
        => Assert.False(ChecklistFormatter.IsMarkdownPath(null));

    [Fact]
    public void SiblingPngPath_Null_Throws()
        => Assert.Throws<ArgumentNullException>(() => ChecklistFormatter.SiblingPngPath(null!));

    [Fact]
    public void SiblingPngPath_UppercaseMd_StillMapsPng()
    {
        using var dir = new TempDir();
        var md = Path.Combine(dir.Path, "notes.MD");
        Assert.Equal(Path.Combine(dir.Path, "notes.png"), ChecklistFormatter.SiblingPngPath(md));
    }

    [Fact]
    public void SiblingPngPath_BareFilename_ReturnsBarePng()
        => Assert.Equal("notes.png", ChecklistFormatter.SiblingPngPath("notes.md"));

    [Fact]
    public void SiblingPngPath_ExtensionOnly_ReturnsNull()
        => Assert.Null(ChecklistFormatter.SiblingPngPath(".md"));

    [Fact]
    public void RelativeImageLink_Blank_Throws()
        => Assert.ThrowsAny<ArgumentException>(() => ChecklistFormatter.RelativeImageLink("  "));

    [Fact]
    public void RelativeImageLink_DirectoryOnly_Throws()
        => Assert.Throws<ArgumentException>(() => ChecklistFormatter.RelativeImageLink(@"C:\temp\"));

    private static ChecklistDocument Sample() => new()
    {
        ExportedAt = ExportedAt,
        SessionType = "Ordinary logon",
        SystemLogNote = "No recent planned or dirty shutdown in the System log.",
        SettledCount = 2,
        TotalCount = 3,
        SessionElapsed = "04:21",
        RestoreRows =
        [
            new ChecklistRow { Name = "Outlook", Source = "Arr", State = AppState.Interactive },
            new ChecklistRow { Name = "Chrome", Source = "Arr", State = AppState.Pending },
        ],
        StartupRows =
        [
            new ChecklistRow { Name = "OneDrive", Source = "Run", State = AppState.Disabled },
            new ChecklistRow { Name = "Dropbox", Source = "Run", State = AppState.Hung },
            new ChecklistRow { Name = "Steam", Source = "Run", State = AppState.Failed },
            new ChecklistRow { Name = "Slack", Source = "Run", State = AppState.Starting },
        ],
        ReentryVersion = "0.1.0-alpha1",
        OsBuild = "Microsoft Windows NT 10.0.26200.0",
        MachineName = "FUNCULAR-WKS-02",
    };
}
