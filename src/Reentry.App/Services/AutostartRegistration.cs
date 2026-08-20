using System.Runtime.InteropServices;
using Reentry.Core.Abstractions;
using Reentry.Core.Inventory;
using Reentry.Core.Models;

namespace Reentry.App.Services;

/// <summary>
/// HKCU Run + a per-user logon task (delay 0, LUA / no highest privileges).
/// Implemented against the Task Scheduler 2.0 COM API (Schedule.Service) so we
/// do not take a third-party NuGet for a one-task write.
/// Disable uses StartupApproved; Cleanup deletes Run, Approved, and the task.
/// </summary>
public sealed class AutostartRegistration : IAutostartRegistration
{
    public const string TaskName = "Reentry";
    private readonly IRegistryWriter _registry;
    private readonly IRegistryReader _reader;

    public AutostartRegistration(Win32Registry registry)
    {
        _registry = registry;
        _reader = registry;
    }

    public bool IsRegistered
    {
        get
        {
            var values = _reader.ReadStringValues(RegistryHiveKind.CurrentUser, StartupRegistryPaths.Run);
            return values is not null && values.ContainsKey(StartupRegistryPaths.ReentryRunValueName);
        }
    }

    public void Register(string executablePath, string arguments = "/autostart")
    {
        var command = $"\"{executablePath}\" {arguments}".Trim();
        _registry.SetStringValue(
            RegistryHiveKind.CurrentUser,
            StartupRegistryPaths.Run,
            StartupRegistryPaths.ReentryRunValueName,
            command);
        _registry.SetBinaryValue(
            RegistryHiveKind.CurrentUser,
            StartupRegistryPaths.ApprovedRun,
            StartupRegistryPaths.ReentryRunValueName,
            StartupApproved.EnabledBytes);

        RegisterTask(executablePath, arguments);
    }

    public void Unregister() => Cleanup();

    public void SetEnabled(bool enabled)
    {
        _registry.SetBinaryValue(
            RegistryHiveKind.CurrentUser,
            StartupRegistryPaths.ApprovedRun,
            StartupRegistryPaths.ReentryRunValueName,
            enabled ? StartupApproved.EnabledBytes : StartupApproved.DisabledBytes);

        TrySetTaskEnabled(enabled);
    }

    public void Cleanup()
    {
        _registry.DeleteValue(RegistryHiveKind.CurrentUser, StartupRegistryPaths.Run, StartupRegistryPaths.ReentryRunValueName);
        _registry.DeleteValue(RegistryHiveKind.CurrentUser, StartupRegistryPaths.ApprovedRun, StartupRegistryPaths.ReentryRunValueName);
        TryDeleteTask();
    }

    private static void RegisterTask(string executablePath, string arguments)
    {
        dynamic? service = null;
        try
        {
            var type = Type.GetTypeFromProgID("Schedule.Service");
            if (type is null)
                return;
            service = Activator.CreateInstance(type);
            service!.Connect();
            dynamic folder = service.GetFolder("\\");
            dynamic definition = service.NewTask(0);

            definition.RegistrationInfo.Description = "Start Reentry at logon so it can show session-restore progress.";
            definition.Principal.LogonType = 3; // TASK_LOGON_INTERACTIVE_TOKEN
            definition.Principal.RunLevel = 0;  // TASK_RUNLEVEL_LUA (not highest)
            definition.Principal.UserId = Environment.UserName;
            definition.Settings.DisallowStartIfOnBatteries = false;
            definition.Settings.StopIfGoingOnBatteries = false;
            definition.Settings.AllowDemandStart = true;
            definition.Settings.StartWhenAvailable = true;
            definition.Settings.ExecutionTimeLimit = "PT0S";
            definition.Settings.Enabled = true;

            dynamic triggers = definition.Triggers;
            dynamic trigger = triggers.Create(9); // TASK_TRIGGER_LOGON
            trigger.UserId = Environment.UserDomainName + "\\" + Environment.UserName;
            trigger.Delay = "PT0S";
            trigger.Enabled = true;

            dynamic actions = definition.Actions;
            dynamic action = actions.Create(0); // TASK_ACTION_EXEC
            action.Path = executablePath;
            action.Arguments = arguments;
            action.WorkingDirectory = Path.GetDirectoryName(executablePath);

            // 6 = TASK_CREATE_OR_UPDATE, 3 = TASK_LOGON_INTERACTIVE_TOKEN
            folder.RegisterTaskDefinition(TaskName, definition, 6, null, null, 3, null);
        }
        catch
        {
            // Run key is enough for autostart; the task is belt-and-suspenders.
        }
        finally
        {
            if (service is not null)
                Marshal.FinalReleaseComObject(service);
        }
    }

    private static void TrySetTaskEnabled(bool enabled)
    {
        dynamic? service = null;
        try
        {
            var type = Type.GetTypeFromProgID("Schedule.Service");
            if (type is null)
                return;
            service = Activator.CreateInstance(type);
            service!.Connect();
            dynamic folder = service.GetFolder("\\");
            dynamic task = folder.GetTask(TaskName);
            task.Enabled = enabled;
        }
        catch
        {
            // missing task is fine
        }
        finally
        {
            if (service is not null)
                Marshal.FinalReleaseComObject(service);
        }
    }

    private static void TryDeleteTask()
    {
        dynamic? service = null;
        try
        {
            var type = Type.GetTypeFromProgID("Schedule.Service");
            if (type is null)
                return;
            service = Activator.CreateInstance(type);
            service!.Connect();
            dynamic folder = service.GetFolder("\\");
            folder.DeleteTask(TaskName, 0);
        }
        catch
        {
            // already gone
        }
        finally
        {
            if (service is not null)
                Marshal.FinalReleaseComObject(service);
        }
    }
}
