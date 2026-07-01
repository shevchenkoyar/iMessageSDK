# iMessageSDK

A production-grade .NET SDK for reading and sending messages through the macOS Messages
application. iMessageSDK models chats, messages, attachments, reactions, and delivery status as a
clean domain — consumers never see SQLite, `chat.db`, AppleScript, or SQL.

> **Platform**: macOS only. iMessageSDK automates the local Messages application and reads its
> conversation history; there is no cross-platform or cloud API for iMessage.

## Packages

| Package | Purpose |
|---|---|
| `iMessageSDK.Abstractions` | Interfaces, records, enums — the public contract. Depend on this alone to mock/test against the SDK. |
| `iMessageSDK` | The concrete implementation (`MessageClient`). |
| `iMessageSDK.Extensions.DependencyInjection` | `AddIMessageSdk()` for `IServiceCollection`. |

## Quickstart

```csharp
await using var client = await MessageClient.CreateAsync();

// Diagnose setup issues before doing anything else.
var report = await client.Diagnostics.RunAsync();
if (!report.IsHealthy)
{
    foreach (var issue in report.Issues)
    {
        Console.WriteLine(issue);
    }

    return;
}

// Fluent querying.
var recent = await client.Messages
    .WhereChat(someChatId)
    .Containing("hello")
    .Take(50)
    .ToListAsync();

// Sending.
await client.Messages.SendTextAsync(someChatId, "Hey!");

// Watching.
var watcher = client.Messages.Watch();
watcher.MessageReceived += (_, e) => Console.WriteLine($"{e.Message.Sender?.DisplayName}: {e.Message.Text}");
await watcher.StartAsync();
```

## Documentation

- [Getting started](docs/getting-started.md)
- [Architecture](docs/architecture.md)
- [Querying](docs/querying.md)
- [Watching](docs/watching.md)
- [Attachments](docs/attachments.md)
- [Diagnostics](docs/diagnostics.md)
- [Permissions setup](docs/permissions.md)
