using Reentry.Core.Boot;
using Reentry.Core.Models;
using Reentry.Core.Tests.Fakes;

namespace Reentry.Core.Tests;

public class BootClassifierTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Classify_User32_1074_IsExpected()
    {
        var reader = new FakeEventLogReader();
        reader.Events.Add(SystemEvent("User32", 1074, Now.AddMinutes(-5)));
        Assert.Equal(BootKind.Expected, new BootClassifier().Classify(reader, Now));
    }

    [Fact]
    public void Classify_Event6008_IsUnexpected()
    {
        var reader = new FakeEventLogReader();
        reader.Events.Add(SystemEvent("EventLog", 6008, Now.AddMinutes(-2)));
        Assert.Equal(BootKind.Unexpected, new BootClassifier().Classify(reader, Now));
    }

    [Fact]
    public void Classify_KernelPower41_IsUnexpected()
    {
        var reader = new FakeEventLogReader();
        reader.Events.Add(SystemEvent("Microsoft-Windows-Kernel-Power", 41, Now.AddMinutes(-1)));
        Assert.Equal(BootKind.Unexpected, new BootClassifier().Classify(reader, Now));
    }

    [Fact]
    public void Classify_MostRecentWins_UnexpectedAfterExpected()
    {
        var reader = new FakeEventLogReader();
        reader.Events.Add(SystemEvent("User32", 1074, Now.AddHours(-2)));
        reader.Events.Add(SystemEvent("Microsoft-Windows-Kernel-Power", 41, Now.AddMinutes(-10)));
        Assert.Equal(BootKind.Unexpected, new BootClassifier().Classify(reader, Now));
    }

    [Fact]
    public void Classify_MostRecentWins_ExpectedAfterUnexpected()
    {
        var reader = new FakeEventLogReader();
        reader.Events.Add(SystemEvent("EventLog", 6008, Now.AddHours(-3)));
        reader.Events.Add(SystemEvent("User32", 1074, Now.AddMinutes(-20)));
        Assert.Equal(BootKind.Expected, new BootClassifier().Classify(reader, Now));
    }

    [Fact]
    public void Classify_NoRelevantEvents_IsOrdinary()
    {
        var reader = new FakeEventLogReader();
        reader.Events.Add(SystemEvent("Service Control Manager", 7036, Now.AddMinutes(-1)));
        Assert.Equal(BootKind.Ordinary, new BootClassifier().Classify(reader, Now));
    }

    [Fact]
    public void Classify_IgnoresEventsOutsideLookback()
    {
        var reader = new FakeEventLogReader();
        reader.Events.Add(SystemEvent("User32", 1074, Now.AddDays(-5)));
        Assert.Equal(BootKind.Ordinary, new BootClassifier().Classify(reader, Now, TimeSpan.FromHours(48)));
    }

    [Fact]
    public void Classify_Event41FromOtherProvider_IsIgnored()
    {
        var reader = new FakeEventLogReader();
        reader.Events.Add(SystemEvent("Some-Other-Provider", 41, Now.AddMinutes(-1)));
        Assert.Equal(BootKind.Ordinary, new BootClassifier().Classify(reader, Now));
    }

    private static EventLogRecord SystemEvent(string provider, int id, DateTimeOffset when) => new()
    {
        LogName = "System",
        Provider = provider,
        EventId = id,
        TimeCreated = when,
    };
}
