# Querying

`client.Messages` and `client.Chats` are themselves fluent query builders (`IMessagesModule`
extends `IMessageQuery`, `IChatsModule` extends `IChatQuery`), so you can start filtering directly:

```csharp
var messages = await client.Messages
    .WhereChat(chat)
    .WhereSender(sender)
    .Containing("hello")
    .Between(from, to)
    .Take(100)
    .ToListAsync();
```

## Immutability

Every filtering/ordering method returns a **new** query instance; the original is never mutated.
This means you can build a "base" query and branch it safely:

```csharp
var baseQuery = client.Messages.WhereChat(chat);

var fromAlice = baseQuery.WhereSender(alice);
var fromBob = baseQuery.WhereSender(bob);
// baseQuery itself still matches every sender in the chat.
```

## Terminal operators

| Method | Behavior |
|---|---|
| `ToListAsync()` | Materializes all matching results into a list. |
| `FirstOrDefaultAsync()` | The first match, or `null`. |
| `CountAsync()` | The number of matches. |
| `AsAsyncEnumerable()` | Streams matches as they are read — use `await foreach` for large result sets. |

Nothing executes until one of these is called.

## Messages: available filters

`WhereChat`, `WhereSender` (by `ParticipantId`, `Participant`, or a raw handle string),
`Containing` (substring search — see note below), `After`/`Before`/`Between`, `OfKind`,
`WithAttachments`, `IncludeDeleted` (unsent messages are excluded by default), `OrderBy`
(`MessageSortOrder`), `Skip`, `Take`.

`IMessagesModule` adds `GetAsync(MessageId)`, `SearchAsync(string)` (shorthand for
`Containing(text).ToListAsync()`), and the send/watch operations described in their own docs.

### A note on `Containing`

A message's searchable text can come from the plain `text` column *or* from best-effort recovery
of Apple's private `attributedBody` archive format (see [attachments.md](attachments.md) for the
same caveat applied to rich text). Because that recovery can't be expressed as SQL, `Containing`
is evaluated against each message's final, resolved text rather than pushed down as a database
`LIKE` filter. In practice this means content search scans candidate rows for the chat/sender/date
range you specified, rather than using a text index — entirely reasonable for a single user's
local history, but not something to run unbounded across years of messages in a hot loop.

## Chats: available filters

`WhereKind` (`ChatKind.Direct` / `ChatKind.Group`), `WhereParticipant`,
`WhereDisplayNameContains`, `OrderByLastActivity`, `Skip`, `Take`.

`IChatsModule` adds `GetAsync(ChatId)`.

## Sort order

Messages default to `MessageSortOrder.SentAtAscending` (chronological reading order). Chats
default to `SortDirection.Descending` on last activity (most recently active first) when you call
`OrderByLastActivity()` with no argument.
