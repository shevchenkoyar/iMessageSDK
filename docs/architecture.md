# Architecture

## Assembly boundaries

```
iMessageSDK.Abstractions   ─ contracts only: interfaces, records, enums, exceptions, event args.
                             No dependency on SQLite or AppleScript. Depend on this alone to mock
                             IMessageClient/IMessagesModule/etc. in your own tests.

iMessageSDK                ─ the concrete implementation. References Abstractions +
                             Microsoft.Data.Sqlite. Its only public type is MessageClient
                             (plus the two options records already in Abstractions). Everything
                             else — database access, row mapping, query translation, attributedBody
                             parsing, AppleScript sending, watching, diagnostics — lives under
                             `internal` types in `iMessageSDK.Internal.*` namespaces and is not
                             visible to consumers.

iMessageSDK.Extensions
  .DependencyInjection      ─ one extension method, AddIMessageSdk, for IServiceCollection.
```

This split mirrors the pattern used by `Microsoft.Extensions.Logging` /
`Microsoft.Extensions.Logging.Abstractions`: the abstractions package is safe to reference from a
class library or test project that should never accidentally pull in a native SQLite dependency.

## Why domain-first

The public API models **messages**, **chats**, **participants**, **attachments**, and
**reactions** — never rows, tables, or SQL. This is deliberate:

- Apple's on-disk schema for `chat.db` is private, undocumented, and has changed shape across
  macOS releases (see `ChatDatabaseSchema`, which introspects columns at runtime rather than
  assuming one fixed layout).
- AppleScript is an implementation detail of *sending*; a future version could add a different
  transport (e.g. a private framework binding) without changing `IMessagesModule` at all.
- Consumers should be able to write unit tests against `iMessageSDK.Abstractions` with a mocking
  library, without ever touching a real database or the real Messages application.

The one deliberate, narrow exception is `IDiagnosticsModule`, which *does* talk about "the
Messages database" and "schema" — because diagnosing setup problems is explicitly a
database-adjacent concern, and hiding that would make the diagnostics feature useless.

## Strongly-typed identifiers

`MessageId`, `ChatId`, `ParticipantId`, and `AttachmentId` each wrap the stable GUID/handle Apple
assigns to that entity — never a SQLite `ROWID`. This keeps identifiers meaningful outside the
context of a single open database connection and prevents accidentally mixing up different kinds
of identifier at compile time.

## Session lifecycle

`MessageClient.CreateAsync` does not open a connection or check any permission; it just resolves
the conversation history path and constructs the four modules. Every query, send, or watch
operation opens its own short-lived, read-only SQLite connection (the real Messages app keeps its
own read-write handle open at all times; SQLite supports any number of concurrent readers against
one writer). `IMessageClient.DisposeAsync` stops and disposes any watchers you created via
`Messages.Watch()` that you have not already disposed yourself.

## Sending

Messages automation always operates on a chat by its stable id and either literal text or a file
already on disk — there is no AppleScript verb for sending an in-memory byte stream, so
`OutgoingAttachment` is deliberately file-path-based. AppleScript's `send` command does not return
the resulting message's identity, so after invoking it, the SDK polls the conversation history
briefly for the newest outgoing message in that chat and returns it as a fully-populated `Message`.

Dynamic values (chat id, message text, file path) are passed to `osascript` as `argv` entries
using an `on run argv` script wrapper — never interpolated into the script source — which
eliminates AppleScript injection risk entirely; see `AppleScriptRunner`.

## Watching

There is no push notification API for new messages, so `IMessageWatcher` combines a
`FileSystemWatcher` on the database's write-ahead log (a fast signal) with a periodic poll (a
reliable fallback). Edits and unsends update an existing row in place rather than inserting a new
one, so the watcher re-examines a trailing time window (comfortably longer than the time macOS
allows a message to be edited or unsent) on every cycle rather than relying purely on a
monotonically increasing cursor.
