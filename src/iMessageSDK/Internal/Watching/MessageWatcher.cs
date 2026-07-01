using iMessageSDK.Attachments;
using iMessageSDK.Internal.Chats;
using iMessageSDK.Internal.Database;
using iMessageSDK.Internal.Querying;
using iMessageSDK.Messages;
using iMessageSDK.Watching;
using Microsoft.Data.Sqlite;

namespace iMessageSDK.Internal.Watching;

/// <summary>
/// Observes the conversation history file for changes by combining a <see cref="FileSystemWatcher"/>
/// on the database's write-ahead log (a fast, low-latency signal) with a periodic poll (a
/// reliable fallback), since Messages exposes no native change-notification API.
/// </summary>
/// <remarks>
/// Edits and unsends update an existing row in place rather than inserting a new one, so they
/// cannot be discovered by only looking at rows newer than a "last seen" cursor. Instead, each
/// poll re-examines all rows from a trailing window (comfortably longer than the time Messages
/// allows a message to be edited or unsent) and compares them against the last-observed state for
/// that message. Event handlers run on a background thread pool thread, consistent with
/// <see cref="FileSystemWatcher"/>'s own convention; marshal to a UI thread yourself if needed.
/// </remarks>
internal sealed class MessageWatcher : IMessageWatcher
{
    // Comfortably longer than the window macOS allows for editing or unsending a message, so a
    // change is never missed just because it fell out of the recheck window.
    private static readonly TimeSpan RecheckWindow = TimeSpan.FromMinutes(30);

    private readonly string _databasePath;
    private readonly TimeSpan _pollingInterval;
    private readonly MessageWatchOptions? _options;
    private readonly SemaphoreSlim _pollGate = new(1, 1);
    private readonly Dictionary<string, WatchedMessageState> _knownMessages = new(StringComparer.Ordinal);
    private readonly HashSet<string> _knownReactionGuids = new(StringComparer.Ordinal);

    private FileSystemWatcher? _fileSystemWatcher;
    private CancellationTokenSource? _runCts;
    private Task? _pollLoopTask;
    private bool _isFirstPoll = true;

    public MessageWatcher(string databasePath, TimeSpan pollingInterval, MessageWatchOptions? options)
    {
        _databasePath = databasePath;
        _pollingInterval = pollingInterval;
        _options = options;
    }

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    public event EventHandler<MessageSentEventArgs>? MessageSent;

    public event EventHandler<MessageEditedEventArgs>? MessageEdited;

    public event EventHandler<MessageDeletedEventArgs>? MessageDeleted;

    public event EventHandler<ReactionAddedEventArgs>? ReactionAdded;

    public event EventHandler<AttachmentDownloadedEventArgs>? AttachmentDownloaded;

    public bool IsRunning { get; private set; }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            return Task.CompletedTask;
        }

        IsRunning = true;
        _isFirstPoll = true;
        _runCts = new CancellationTokenSource();

        var directory = Path.GetDirectoryName(_databasePath);
        var fileName = Path.GetFileName(_databasePath);
        if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
        {
            _fileSystemWatcher = new FileSystemWatcher(directory)
            {
                Filter = fileName + "*",
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
            };
            _fileSystemWatcher.Changed += OnDatabaseFileChanged;
            _fileSystemWatcher.EnableRaisingEvents = true;
        }

        _pollLoopTask = Task.Run(() => PollLoopAsync(_runCts.Token), CancellationToken.None);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;

        if (_fileSystemWatcher is not null)
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            _fileSystemWatcher.Changed -= OnDatabaseFileChanged;
            _fileSystemWatcher.Dispose();
            _fileSystemWatcher = null;
        }

        if (_runCts is not null)
        {
            await _runCts.CancelAsync().ConfigureAwait(false);
        }

        if (_pollLoopTask is not null)
        {
            try
            {
                await _pollLoopTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation.
            }
        }

        _runCts?.Dispose();
        _runCts = null;
        _pollLoopTask = null;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
        _pollGate.Dispose();
    }

    private void OnDatabaseFileChanged(object sender, FileSystemEventArgs e) => TriggerPoll();

    private void TriggerPoll()
    {
        var token = _runCts?.Token ?? CancellationToken.None;
        _ = Task.Run(() => PollOnceGuardedAsync(token), CancellationToken.None);
    }

    private async Task PollLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await PollOnceGuardedAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                await Task.Delay(_pollingInterval, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task PollOnceGuardedAsync(CancellationToken cancellationToken)
    {
        if (!await _pollGate.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            await PollOnceAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // A transient failure (e.g. the database file briefly locked mid-write) should not
            // stop the watcher; the next cycle retries.
        }
        finally
        {
            _pollGate.Release();
        }
    }

    private async Task PollOnceAsync(CancellationToken cancellationToken)
    {
        await using var connection = await ChatDatabaseConnectionFactory.OpenAsync(_databasePath, cancellationToken).ConfigureAwait(false);
        var schema = await ChatDatabaseSchema.LoadAsync(connection, cancellationToken).ConfigureAwait(false);
        var handleDirectory = await HandleDirectory.LoadAsync(connection, cancellationToken).ConfigureAwait(false);

        var since = AppleTimeConverter.ToAppleNanoseconds(DateTimeOffset.UtcNow - RecheckWindow);
        var rows = await FetchRecentRowsAsync(connection, schema, since, cancellationToken).ConfigureAwait(false);

        // On the very first poll after starting, establish the baseline silently: only changes
        // observed on subsequent polls are reported as events.
        var suppressEvents = _isFirstPoll;
        _isFirstPoll = false;

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (MessageRowMapper.IsReactionRow(row))
            {
                await ProcessReactionRowAsync(connection, schema, handleDirectory, row, suppressEvents, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await ProcessMessageRowAsync(connection, schema, handleDirectory, row, suppressEvents, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private async Task ProcessMessageRowAsync(
        SqliteConnection connection, ChatDatabaseSchema schema, HandleDirectory handleDirectory, MessageRow row, bool suppressEvents, CancellationToken cancellationToken)
    {
        var isNew = !_knownMessages.TryGetValue(row.Guid, out var previousState);

        if (!isNew
            && previousState!.DateEdited == row.DateEdited
            && previousState.DateRetracted == row.DateRetracted
            && row.CacheHasAttachments is false)
        {
            // Nothing tracked could have changed; skip the cost of full assembly.
            return;
        }

        var message = await MessageRowAssembler.AssembleAsync(connection, schema, handleDirectory, row, cancellationToken).ConfigureAwait(false);
        var attachmentStates = message.Attachments.ToDictionary(a => a.Id, a => a.TransferState);

        if (!suppressEvents)
        {
            if (isNew)
            {
                if (message.IsFromMe)
                {
                    MessageSent?.Invoke(this, new MessageSentEventArgs { Message = message });
                }
                else
                {
                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs { Message = message });
                }
            }
            else
            {
                if (row.DateEdited is not null && row.DateEdited != previousState!.DateEdited)
                {
                    MessageEdited?.Invoke(this, new MessageEditedEventArgs { Message = message, PreviousText = previousState.Text });
                }

                if (row.DateRetracted is not null && row.DateRetracted != previousState!.DateRetracted)
                {
                    MessageDeleted?.Invoke(this, new MessageDeletedEventArgs
                    {
                        MessageId = message.Id,
                        ChatId = message.ChatId,
                        DeletedAt = message.DeletionInfo?.DeletedAt ?? DateTimeOffset.UtcNow,
                    });
                }

                foreach (var (attachmentId, state) in attachmentStates)
                {
                    if (state == AttachmentTransferState.Downloaded
                        && previousState!.AttachmentStates.GetValueOrDefault(attachmentId) != AttachmentTransferState.Downloaded)
                    {
                        var attachment = message.Attachments.First(a => a.Id == attachmentId);
                        AttachmentDownloaded?.Invoke(this, new AttachmentDownloadedEventArgs { Attachment = attachment, MessageId = message.Id });
                    }
                }
            }
        }

        _knownMessages[row.Guid] = new WatchedMessageState(message.Text, row.DateEdited, row.DateRetracted, attachmentStates);
    }

    private Task ProcessReactionRowAsync(
        SqliteConnection connection, ChatDatabaseSchema schema, HandleDirectory handleDirectory, MessageRow row, bool suppressEvents, CancellationToken cancellationToken)
    {
        if (!_knownReactionGuids.Add(row.Guid))
        {
            return Task.CompletedTask;
        }

        // Only "add" events (not retractions) are surfaced; the abstraction intentionally has no
        // ReactionRemoved event, matching the required event set.
        if (!suppressEvents && row.AssociatedMessageType is >= 2000 and <= 2006 && row.AssociatedMessageGuid is not null)
        {
            var sender = handleDirectory.ResolveSender(row.HandleId, row.IsFromMe);
            var reaction = Internal.Messages.MessageMapper.MapReaction(row, sender);
            var targetGuid = MessageRowMapper.ExtractTargetMessageGuid(row.AssociatedMessageGuid);
            ReactionAdded?.Invoke(this, new ReactionAddedEventArgs { Reaction = reaction, TargetMessageId = new MessageId(targetGuid) });
        }

        return Task.CompletedTask;
    }

    private async Task<List<MessageRow>> FetchRecentRowsAsync(
        SqliteConnection connection, ChatDatabaseSchema schema, long sinceNanoseconds, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
            {MessageRowMapper.BuildSelectColumns(schema)}
            FROM message m
            JOIN chat_message_join cmj ON cmj.message_id = m.ROWID
            JOIN chat c ON c.ROWID = cmj.chat_id
            WHERE m.date >= @since
            {(_options?.ChatId is not null ? "AND c.guid = @chatGuid" : string.Empty)}
            ORDER BY m.date ASC
            """;
        command.Parameters.AddWithValue("@since", sinceNanoseconds);
        if (_options?.ChatId is { } chatId)
        {
            command.Parameters.AddWithValue("@chatGuid", chatId.Value);
        }

        var rows = new List<MessageRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            rows.Add(MessageRowMapper.ReadRow(reader));
        }

        return rows;
    }
}
