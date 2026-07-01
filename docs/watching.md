# Watching

`client.Messages.Watch()` returns an `IMessageWatcher` that raises a typed event for each kind of
change. It does not start observing until you call `StartAsync`:

```csharp
var watcher = client.Messages.Watch(); // optionally: Watch(new MessageWatchOptions { ChatId = chat.Id })

watcher.MessageReceived += (_, e) => Console.WriteLine($"{e.Message.Sender?.DisplayName}: {e.Message.Text}");
watcher.MessageSent += (_, e) => Console.WriteLine($"Sent: {e.Message.Text}");
watcher.MessageEdited += (_, e) => Console.WriteLine($"Edited (was: {e.PreviousText}): {e.Message.Text}");
watcher.MessageDeleted += (_, e) => Console.WriteLine($"Unsent: {e.MessageId}");
watcher.ReactionAdded += (_, e) => Console.WriteLine($"{e.Reaction.Sender.DisplayName} reacted {e.Reaction.Kind}");
watcher.AttachmentDownloaded += (_, e) => Console.WriteLine($"Ready: {e.Attachment.FileName}");

await watcher.StartAsync();

// ... later
await watcher.DisposeAsync(); // equivalent to StopAsync, and safe to call even if already stopped
```

## Lifecycle

- Multiple independent watchers can run at once (e.g. one per chat you care about, via
  `MessageWatchOptions.ChatId`).
- Event handlers run on a background thread pool thread — the same convention as
  `FileSystemWatcher`. Marshal to your UI thread yourself if you're updating UI state.
- Disposing `IMessageClient` stops and disposes any watcher you created that you haven't already
  disposed yourself; you do not need to track them manually just to avoid a leak on shutdown.
- The very first poll after `StartAsync` establishes a baseline silently — you only get events for
  changes that happen *after* you start watching, not a backlog of everything already in the
  chat's history.

## Why polling

Messages exposes no push-notification API for new content. `IMessageWatcher` combines a
`FileSystemWatcher` on the conversation history's write-ahead log (a fast signal that something
changed) with a periodic poll on `MessageClientOptions.PollingInterval` as a reliable fallback.
Edits and unsends update an existing row rather than inserting a new one, so each poll re-examines
a trailing ~30 minute window (comfortably longer than the time macOS allows a message to be edited
or unsent) rather than only looking at rows newer than a "last seen" cursor.

## Reactions

`ReactionAdded` fires for a genuinely new tapback being applied. It intentionally does not fire
for a tapback being retracted — there is no `ReactionRemoved` event — but a retraction is reflected
the next time you read the message: `Message.Reactions` always represents the *current* set of
active reactions, not a raw event log.
