namespace Reentry.Core.Inventory;

public static class StartupRegistryPaths
{
    public const string Run = @"Software\Microsoft\Windows\CurrentVersion\Run";
    public const string RunOnce = @"Software\Microsoft\Windows\CurrentVersion\RunOnce";
    public const string Wow64Run = @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\Run";
    public const string Wow64RunOnce = @"Software\WOW6432Node\Microsoft\Windows\CurrentVersion\RunOnce";

    public const string ApprovedRun = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    public const string ApprovedRun32 = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32";
    public const string ApprovedStartupFolder = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";

    public const string ReentryRunValueName = "Reentry";
}
