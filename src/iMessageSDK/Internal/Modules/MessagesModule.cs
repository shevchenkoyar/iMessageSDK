using iMessageSDK.Chats;
using iMessageSDK.Internal.Querying;
using iMessageSDK.Internal.Sending;
using iMessageSDK.Internal.Watching;
using iMessageSDK.Messages;
using iMessageSDK.Watching;

namespace iMessageSDK.Internal.Modules;

/// <summary>The concrete <see cref="IMessagesModule"/> implementation, combining fluent querying, sending, and watching.</summary>
internal sealed class MessagesModule : MessageQuery, IMessagesModule, IAsyncDisposable
{
    private readonly MessageQueryExecutor _executor;
    private readonly AppleScriptMessageSender _sender;
    private readonly string _databasePath;
    private readonly TimeSpan _pollingInterval;
    private readonly List<IMessageWatcher> _watchers = [];
    private readonly Lock _watchersLock = new();

    public MessagesModule(string databasePath, TimeSpan pollingInterval)
        : this(new MessageQueryExecutor(databasePath), databasePath, pollingInterval)
    {
    }

    private MessagesModule(MessageQueryExecutor executor, string databasePath, TimeSpan pollingInterval)
        : base(executor, new MessageQueryCriteria())
    {
        _executor = executor;
        _sender = new AppleScriptMessageSender(databasePath);
        _databasePath = databasePath;
        _pollingInterval = pollingInterval;
    }

    public Task<Message?> GetAsync(MessageId id, CancellationToken cancellationToken = default) =>
        _executor.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Message>> SearchAsync(string text, CancellationToken cancellationToken = default) =>
        Containing(text).ToListAsync(cancellationToken);

    public Task<Message> SendAsync(ChatId chatId, MessageContent content, CancellationToken cancellationToken = default) =>
        _sender.SendAsync(chatId, content, cancellationToken);

    public Task<Message> SendTextAsync(ChatId chatId, string text, CancellationToken cancellationToken = default) =>
        SendAsync(chatId, MessageContent.FromText(text), cancellationToken);

    public Task<Message> SendAttachmentAsync(ChatId chatId, OutgoingAttachment attachment, string? text = null, CancellationToken cancellationToken = default) =>
        SendAsync(chatId, MessageContent.FromAttachment(attachment, text), cancellationToken);

    public IMessageWatcher Watch(MessageWatchOptions? options = null)
    {
        var watcher = new MessageWatcher(_databasePath, _pollingInterval, options);
        lock (_watchersLock)
        {
            _watchers.Add(watcher);
        }

        return watcher;
    }

    /// <summary>Stops and disposes every watcher this module has created that the caller has not already disposed.</summary>
    public async ValueTask DisposeAsync()
    {
        List<IMessageWatcher> snapshot;
        lock (_watchersLock)
        {
            snapshot = [.. _watchers];
            _watchers.Clear();
        }

        foreach (var watcher in snapshot)
        {
            await watcher.DisposeAsync().ConfigureAwait(false);
        }
    }
}
