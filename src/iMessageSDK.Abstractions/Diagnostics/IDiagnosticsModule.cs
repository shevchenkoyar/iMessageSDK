namespace iMessageSDK.Diagnostics;

/// <summary>
/// The entry point for diagnosing whether the SDK can operate on the current machine: macOS
/// permissions (Full Disk Access, Automation), Messages application availability, and the health
/// of the underlying conversation history.
/// </summary>
/// <remarks>
/// This interface is intended to be consumed, not implemented, by SDK users; new members may be
/// added in future minor versions without that being considered a breaking change.
/// </remarks>
public interface IDiagnosticsModule
{
    /// <summary>Runs every check and returns an aggregate report.</summary>
    Task<SdkDiagnosticsReport> RunAsync(CancellationToken cancellationToken = default);

    /// <summary>Checks whether the process has Full Disk Access, required to read conversation history.</summary>
    Task<PermissionCheckResult> CheckFullDiskAccessAsync(CancellationToken cancellationToken = default);

    /// <summary>Checks whether the process has permission to automate the Messages application, required to send messages.</summary>
    Task<PermissionCheckResult> CheckAutomationPermissionAsync(CancellationToken cancellationToken = default);

    /// <summary>Checks the availability of the Messages application itself.</summary>
    Task<MessagesAppAvailability> CheckMessagesAppAvailabilityAsync(CancellationToken cancellationToken = default);

    /// <summary>Checks the health of the underlying conversation history.</summary>
    Task<MessagesDatabaseDiagnostics> CheckDatabaseAsync(CancellationToken cancellationToken = default);
}
