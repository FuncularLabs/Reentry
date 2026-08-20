using Reentry.Core;

namespace Reentry.Core.Tests;

public class CommandTextTests
{
    [Theory]
    [InlineData("\"C:\\Program Files\\Steam\\steam.exe\" -silent", "C:\\Program Files\\Steam\\steam.exe")]
    [InlineData("C:\\Program Files\\Steam\\steam.exe -silent", "C:\\Program Files\\Steam\\steam.exe")]
    [InlineData("C:\\Windows\\System32\\SecurityHealthSystray.exe", "C:\\Windows\\System32\\SecurityHealthSystray.exe")]
    [InlineData("notes.exe", "notes.exe")]
    public void ExtractExecutable_HandlesQuotedAndUnquoted(string command, string expected)
        => Assert.Equal(expected, CommandText.ExtractExecutable(command));

    [Fact]
    public void SameExecutable_IgnoresPathAndExtension()
    {
        Assert.True(CommandText.SameExecutable(
            "C:\\Program Files\\Steam\\steam.exe -silent",
            "steam.exe"));
        Assert.True(CommandText.SameExecutable(
            "C:\\Program Files\\Microsoft Office\\OUTLOOK.EXE",
            "outlook"));
    }
}
