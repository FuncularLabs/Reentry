namespace Reentry.Core.Abstractions;

/// <summary>
/// Register Reentry so it starts first among user apps at logon.
/// Implementation (App): HKCU Run + a per-user logon task (delay 0, no highest privileges).
/// Honor disable via StartupApproved — do not delete the Run value.
/// </summary>
public interface IAutostartRegistration
{
    bool IsRegistered { get; }

    void Register(string executablePath, string arguments = "/autostart");

    void Unregister();

    /// <summary>Enable or disable without deleting the Run value or the task definition.</summary>
    void SetEnabled(bool enabled);

    /// <summary>Uninstall: delete Run, StartupApproved, and the scheduled task.</summary>
    void Cleanup();
}
