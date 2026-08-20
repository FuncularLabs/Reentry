using Reentry.Core.Models;

namespace Reentry.Core.Tests;

public class AppStateTests
{
    [Theory]
    [InlineData(AppState.Interactive, true)]
    [InlineData(AppState.Failed, true)]
    [InlineData(AppState.Hung, true)]
    [InlineData(AppState.Disabled, true)]
    [InlineData(AppState.Pending, false)]
    [InlineData(AppState.Starting, false)]
    public void IsSettled_TerminalStatesOnly(AppState state, bool settled)
        => Assert.Equal(settled, state.IsSettled());
}
