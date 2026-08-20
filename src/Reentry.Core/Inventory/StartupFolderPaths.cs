namespace Reentry.Core.Inventory;

public sealed class StartupFolderPaths
{
    public string UserStartup { get; init; } = "";
    public string CommonStartup { get; init; } = "";

    public static StartupFolderPaths FromEnvironment() => new()
    {
        UserStartup = Environment.GetFolderPath(Environment.SpecialFolder.Startup),
        CommonStartup = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup),
    };
}
