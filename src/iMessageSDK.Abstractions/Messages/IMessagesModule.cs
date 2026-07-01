using iMessageSDK.Chats;
using iMessageSDK.Watching;

namespace iMessageSDK.Messages;

/// <summary>
/// The entry point for reading, searching, sending, and watching messages.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IMessagesModule"/> extends <see cref="IMessageQuery"/> so queries can be started
/// directly from <c>client.Messages</c>, for example:
/// <code>
/// var messages = await client.Messages
///     .WhereChat(chat)
///     .Containing("hello")
///     .Take(100)
///     .ToListAsync();
/// </code>
/// </para>
/// <para>
/// This interface is intended to be consumed, not implemented, by SDK users; new members may be
/// added in future minor versions (for example, to support replying, reacting, editing, or
/// deleting) without that being considered a breaking change.
/// </para>
/// </remarks>
public interface IMessagesModule : IMessageQuery
{
    /// <summary>Retrieves a single message by its identifier, or <see langword="null"/> if it does not exist.</summary>
    Task<Message?> GetAsync(MessageId id, CancellationToken cancellationToken = default);

    /// <summary>Searches for messages whose text contains the given substring, across all chats.</summary>
    /// <remarks>Equivalent to <c>Containing(text).ToListAsync()</c>.</remarks>
    Task<IReadOnlyList<Message>> SearchAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>Sends a new message to a chat.</summary>
    /// <param name="chatId">The chat to send to.</param>
    /// <param name="content">The text, attachments, or both to send.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    Task<Message> SendAsync(ChatId chatId, MessageContent content, CancellationToken cancellationToken = default);

    /// <summary>Sends a text-only message to a chat.</summary>
    Task<Message> SendTextAsync(ChatId chatId, string text, CancellationToken cancellationToken = default);

    /// <summary>Sends a message carrying a single attachment, with optional accompanying text, to a chat.</summary>
    Task<Message> SendAttachmentAsync(ChatId chatId, OutgoingAttachment attachment, string? text = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a watcher that raises events as new messages, reactions, edits, deletions, and
    /// attachment downloads occur. The watcher does not start observing until
    /// <see cref="IMessageWatcher.StartAsync"/> is called.
    /// </summary>
    /// <param name="options">Optional filtering options; <see langword="null"/> observes all chats.</param>
    IMessageWatcher Watch(MessageWatchOptions? options = null);
}
