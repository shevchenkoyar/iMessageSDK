namespace iMessageSDK.Watching;

/// <summary>
/// Observes a chat (or all chats) for new messages, reactions, edits, deletions, and attachment
/// downloads, raising a typed event for each.
/// </summary>
/// <remarks>
/// <para>
/// Created via <c>IMessagesModule.Watch</c>. Subscribe to the events you care about, then call
/// <see cref="StartAsync"/> to begin observing:
/// <code>
/// using var watcher = client.Messages.Watch();
/// watcher.MessageReceived += (_, e) => Console.WriteLine(e.Message.Text);
/// await watcher.StartAsync();
/// </code>
/// </para>
/// <para>
/// Disposing the watcher stops observation and releases its underlying resources; it is
/// equivalent to calling <see cref="StopAsync"/>.
/// </para>
/// </remarks>
public interface IMessageWatcher : IAsyncDisposable
{
    /// <summary>Raised when a new message is received from another participant.</summary>
    event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    /// <summary>Raised when a new message is sent from the local account.</summary>
    event EventHandler<MessageSentEventArgs>? MessageSent;

    /// <summary>Raised when a message's text is changed after it was sent.</summary>
    event EventHandler<MessageEditedEventArgs>? MessageEdited;

    /// <summary>Raised when a message is unsent (deleted) after it was sent.</summary>
    event EventHandler<MessageDeletedEventArgs>? MessageDeleted;

    /// <summary>Raised when a tapback reaction is applied to a message.</summary>
    event EventHandler<ReactionAddedEventArgs>? ReactionAdded;

    /// <summary>Raised when a previously pending attachment finishes downloading and becomes available.</summary>
    event EventHandler<AttachmentDownloadedEventArgs>? AttachmentDownloaded;

    /// <summary><see langword="true"/> if this watcher is currently observing for changes.</summary>
    bool IsRunning { get; }

    /// <summary>Begins observing for changes, raising events as they occur.</summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>Stops observing for changes. The watcher can be restarted with a subsequent call to <see cref="StartAsync"/>.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
