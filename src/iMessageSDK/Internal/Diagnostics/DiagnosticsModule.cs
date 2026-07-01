using iMessageSDK.Diagnostics;
using iMessageSDK.Internal.Database;
using iMessageSDK.Internal.Sending;
using Microsoft.Data.Sqlite;

namespace iMessageSDK.Internal.Diagnostics;

/// <summary>
/// Diagnoses whether the SDK can operate on the current machine: Full Disk Access, Automation
/// permission for Messages, Messages application availability, and the health of the
/// conversation history file.
/// </summary>
internal sealed class DiagnosticsModule : IDiagnosticsModule
{
    private readonly string _databasePath;

    public DiagnosticsModule(string databasePath)
    {
        _databasePath = databasePath;
    }

    public async Task<SdkDiagnosticsReport> RunAsync(CancellationToken cancellationToken = default)
    {
        var fullDiskAccess = await CheckFullDiskAccessAsync(cancellationToken).ConfigureAwait(false);
        var automation = await CheckAutomationPermissionAsync(cancellationToken).ConfigureAwait(false);
        var appAvailability = await CheckMessagesAppAvailabilityAsync(cancellationToken).ConfigureAwait(false);
        var database = await CheckDatabaseAsync(cancellationToken).ConfigureAwait(false);

        var issues = new List<string>();
        if (!fullDiskAccess.IsGranted)
        {
            issues.Add(fullDiskAccess.Details ?? "Full Disk Access has not been granted.");
        }

        if (!automation.IsGranted)
        {
            issues.Add(automation.Details ?? "Automation access to Messages has not been granted.");
        }

        if (!appAvailability.IsInstalled)
        {
            issues.Add("The Messages application is not installed.");
        }

        if (!database.Exists)
        {
            issues.Add("The conversation history file was not found.");
        }
        else if (!database.IsReadable)
        {
            issues.Add("The conversation history file could not be read.");
        }
        else if (!database.IsSchemaSupported)
        {
            issues.Add("The conversation history file's structure was not recognized by this version of the SDK.");
        }

        return new SdkDiagnosticsReport
        {
            FullDiskAccess = fullDiskAccess,
            AutomationPermission = automation,
            MessagesAppAvailability = appAvailability,
            Database = database,
            Issues = issues,
        };
    }

    public async Task<PermissionCheckResult> CheckFullDiskAccessAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_databasePath))
        {
            return new PermissionCheckResult(false, $"The conversation history file was not found at '{_databasePath}'.");
        }

        try
        {
            await using var connection = await ChatDatabaseConnectionFactory.OpenAsync(_databasePath, cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master";
            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return new PermissionCheckResult(true, null);
        }
        catch (Exception ex) when (ex is SqliteException or UnauthorizedAccessException)
        {
            return new PermissionCheckResult(
                false,
                $"Full Disk Access has not been granted to this process. Grant it under System Settings > Privacy & Security > Full Disk Access. Details: {ex.Message}");
        }
    }

    public async Task<PermissionCheckResult> CheckAutomationPermissionAsync(CancellationToken cancellationToken = default)
    {
        var result = await AppleScriptRunner.RunAsync(AppleScriptTemplates.GetMessagesApplicationName, [], cancellationToken).ConfigureAwait(false);
        if (result.Succeeded)
        {
            return new PermissionCheckResult(true, null);
        }

        if (result.IsAutomationPermissionDenied)
        {
            return new PermissionCheckResult(
                false,
                $"Automation access to control \"Messages\" has not been granted to this process. Grant it under System Settings > Privacy & Security > Automation. Details: {result.StandardError.Trim()}");
        }

        return new PermissionCheckResult(false, $"Could not verify Automation access: {result.StandardError.Trim()}");
    }

    public async Task<MessagesAppAvailability> CheckMessagesAppAvailabilityAsync(CancellationToken cancellationToken = default)
    {
        var isInstalled = Directory.Exists("/System/Applications/Messages.app") || Directory.Exists("/Applications/Messages.app");

        var isRunning = false;
        if (isInstalled)
        {
            var result = await AppleScriptRunner.RunAsync(
                    """tell application "System Events" to (name of processes) contains "Messages" """,
                    [],
                    cancellationToken)
                .ConfigureAwait(false);
            isRunning = result.Succeeded && result.StandardOutput.Trim().Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        return new MessagesAppAvailability(isInstalled, isRunning);
    }

    public async Task<MessagesDatabaseDiagnostics> CheckDatabaseAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_databasePath))
        {
            return new MessagesDatabaseDiagnostics(Exists: false, IsReadable: false, IsSchemaSupported: false);
        }

        try
        {
            await using var connection = await ChatDatabaseConnectionFactory.OpenAsync(_databasePath, cancellationToken).ConfigureAwait(false);
            var schema = await ChatDatabaseSchema.LoadAsync(connection, cancellationToken).ConfigureAwait(false);
            return new MessagesDatabaseDiagnostics(Exists: true, IsReadable: true, IsSchemaSupported: schema.IsRecognizedSchema);
        }
        catch (Exception ex) when (ex is SqliteException or UnauthorizedAccessException or IOException)
        {
            return new MessagesDatabaseDiagnostics(Exists: true, IsReadable: false, IsSchemaSupported: false);
        }
    }
}
