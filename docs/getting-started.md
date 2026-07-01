# Getting Started

## Install

```bash
dotnet add package iMessageSDK
# optional, for DI-based apps:
dotnet add package iMessageSDK.Extensions.DependencyInjection
```

## Prerequisites

iMessageSDK automates the real Messages application and reads its local conversation history.
Before it can do anything useful, the host process needs two macOS permissions:

1. **Full Disk Access** — to read `~/Library/Messages/chat.db`.
2. **Automation** access to control "Messages" — to send messages.

See [permissions.md](permissions.md) for exact steps. You do not need to request these
permissions programmatically; the SDK's [diagnostics](diagnostics.md) module tells you which, if
any, are missing.

## Creating a client

```csharp
using iMessageSDK;

await using var client = await MessageClient.CreateAsync();
```

`CreateAsync` throws `UnsupportedPlatformException` if called on a non-macOS OS. It does not
open any connection or check any permission itself — those happen lazily, the first time you
query, send, or watch. Run diagnostics first if you want to fail fast with a clear message:

```csharp
var report = await client.Diagnostics.RunAsync();
if (!report.IsHealthy)
{
    throw new InvalidOperationException(string.Join(Environment.NewLine, report.Issues));
}
```

## Your first query and send

```csharp
var chats = await client.Chats.OrderByLastActivity().Take(10).ToListAsync();
var mostRecentChat = chats[0];

var messages = await client.Messages
    .WhereChat(mostRecentChat)
    .OrderBy(MessageSortOrder.SentAtDescending)
    .Take(20)
    .ToListAsync();

await client.Messages.SendTextAsync(mostRecentChat.Id, "Hello from iMessageSDK!");
```

## Dependency injection

```csharp
services.AddIMessageSdk(options =>
{
    options.PollingInterval = TimeSpan.FromSeconds(1);
});
```

Then inject `IMessageClient` wherever you need it. See [architecture.md](architecture.md) for the
tradeoffs of this registration versus calling `MessageClient.CreateAsync` directly.

## Next steps

- [Querying](querying.md) — the fluent query builder in depth.
- [Watching](watching.md) — observing new messages, reactions, edits, and deletions.
- [Attachments](attachments.md) — reading metadata and opening content streams.
- [Diagnostics](diagnostics.md) — troubleshooting setup issues.
