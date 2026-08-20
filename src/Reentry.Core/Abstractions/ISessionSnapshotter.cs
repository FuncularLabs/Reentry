using Reentry.Core.Models;

namespace Reentry.Core.Abstractions;

/// <summary>
/// Capture a last-session snapshot. The ENDSESSION / timer hook lives in the App;
/// Core only defines the contract and a probe-backed default implementation.
/// </summary>
public interface ISessionSnapshotter
{
    SessionSnapshot Capture();
}
