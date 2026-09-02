using Reentry.Core.Timing;

namespace Reentry.App.ViewModels;

internal static class DurationFormat
{
    public static string Clock(TimeSpan elapsed)
    {
        if (elapsed.TotalHours >= 1)
            return $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}";
        return $"{elapsed.Minutes:00}:{elapsed.Seconds:00}";
    }

    public static string Section(TimingTriple t)
    {
        var parts = new List<string>(3);
        if (t.Last is { } last)
            parts.Add($"last {Clock(last)}");
        parts.Add($"now {Clock(t.Current)}");
        if (t.Average is { } avg)
            parts.Add($"avg {Clock(avg)}");
        return string.Join("  ", parts);
    }

    public static string Row(TimingTriple t)
    {
        if (t.Last is null && t.Average is null)
            return Clock(t.Current);

        var last = t.Last is { } l ? Clock(l) : "—";
        var avg = t.Average is { } a ? Clock(a) : "—";
        return $"{last} · {Clock(t.Current)} · {avg}";
    }
}
