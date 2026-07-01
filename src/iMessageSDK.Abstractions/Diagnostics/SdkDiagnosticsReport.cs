namespace iMessageSDK.Diagnostics;

/// <summary>
/// An aggregate report describing whether the SDK is able to fully operate on the current
/// machine.
/// </summary>
public sealed record SdkDiagnosticsReport
{
    /// <summary>Whether the process has Full Disk Access, required to read conversation history.</summary>
    public required PermissionCheckResult FullDiskAccess { get; init; }

    /// <summary>Whether the process has permission to automate the Messages application, required to send messages.</summary>
    public required PermissionCheckResult AutomationPermission { get; init; }

    /// <summary>The availability of the Messages application itself.</summary>
    public required MessagesAppAvailability MessagesAppAvailability { get; init; }

    /// <summary>The health of the underlying conversation history.</summary>
    public required MessagesDatabaseDiagnostics Database { get; init; }

    /// <summary>
    /// <see langword="true"/> if every check passed and the SDK is expected to fully operate.
    /// </summary>
    public bool IsHealthy =>
        FullDiskAccess.IsGranted
        && AutomationPermission.IsGranted
        && MessagesAppAvailability.IsInstalled
        && Database is { Exists: true, IsReadable: true, IsSchemaSupported: true };

    /// <summary>Human-readable descriptions of every check that did not pass, empty when <see cref="IsHealthy"/> is <see langword="true"/>.</summary>
    public IReadOnlyList<string> Issues { get; init; } = [];
}
