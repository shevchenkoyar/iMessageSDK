# Diagnostics

Almost every problem consumers hit with this SDK is a setup problem, not a code problem: a missing
permission, Messages not being installed, or an unreadable conversation history file.
`client.Diagnostics` exists to make those problems immediately visible instead of surfacing as a
confusing exception three layers deep.

```csharp
var report = await client.Diagnostics.RunAsync();

if (!report.IsHealthy)
{
    foreach (var issue in report.Issues)
    {
        Console.WriteLine(issue);
    }
}
```

`SdkDiagnosticsReport` aggregates four checks, each also available individually:

| Check | Method | What it means if it fails |
|---|---|---|
| Full Disk Access | `CheckFullDiskAccessAsync` | Reading messages will throw `FullDiskAccessDeniedException`. |
| Automation permission | `CheckAutomationPermissionAsync` | Sending will throw `AutomationPermissionDeniedException`. |
| Messages availability | `CheckMessagesAppAvailabilityAsync` | Messages isn't installed, or isn't currently running. |
| Conversation history health | `CheckDatabaseAsync` | The file is missing, unreadable, or has an unrecognized internal structure. |

See [permissions.md](permissions.md) for how to grant the two macOS permissions — the SDK cannot
grant them on your behalf; macOS requires the user to do that through System Settings.

## Why this doesn't leak database concepts

The rest of the SDK never mentions SQLite, `chat.db`, or schemas — but diagnostics deliberately
does talk about "the conversation history" and "schema support", because that *is* the concern
this module exists to surface. `MessagesDatabaseDiagnostics` is named and documented to make clear
this is a narrow, intentional exception for troubleshooting, not a crack in the domain model.
