using iMessageSDK.Chats;

namespace iMessageSDK.Messages;

/// <summary>
/// A composable, immutable query over messages. Each filtering or ordering method returns a new
/// <see cref="IMessageQuery"/> reflecting the added criterion; the original instance is
/// unaffected, so queries can be freely branched and reused.
/// </summary>
/// <remarks>
/// Terminal methods (<see cref="ToListAsync"/>, <see cref="FirstOrDefaultAsync"/>,
/// <see cref="CountAsync"/>, <see cref="AsAsyncEnumerable"/>) execute the query. Nothing runs
/// until one of them is called.
/// </remarks>
public interface IMessageQuery
{
    /// <summary>Restricts results to messages belonging to the given chat.</summary>
    IMessageQuery WhereChat(ChatId chatId);

    /// <summary>Restricts results to messages belonging to the given chat.</summary>
    IMessageQuery WhereChat(Chat chat);

    /// <summary>Restricts results to messages sent by the given participant.</summary>
    IMessageQuery WhereSender(ParticipantId participantId);

    /// <summary>Restricts results to messages sent by the given participant.</summary>
    IMessageQuery WhereSender(Participant participant);

    /// <summary>Restricts results to messages sent by the participant with the given handle (phone number or email).</summary>
    IMessageQuery WhereSender(string handle);

    /// <summary>Restricts results to messages whose text contains the given substring.</summary>
    /// <param name="text">The substring to search for.</param>
    /// <param name="comparison">The string comparison to use when matching.</param>
    IMessageQuery Containing(string text, StringComparison comparison = StringComparison.OrdinalIgnoreCase);

    /// <summary>Restricts results to messages sent at or after the given moment.</summary>
    IMessageQuery After(DateTimeOffset timestamp);

    /// <summary>Restricts results to messages sent at or before the given moment.</summary>
    IMessageQuery Before(DateTimeOffset timestamp);

    /// <summary>Restricts results to messages sent between the given moments, inclusive.</summary>
    IMessageQuery Between(DateTimeOffset from, DateTimeOffset to);

    /// <summary>Restricts results to messages of the given kind.</summary>
    IMessageQuery OfKind(MessageKind kind);

    /// <summary>Restricts results to messages that carry at least one attachment.</summary>
    IMessageQuery WithAttachments();

    /// <summary>
    /// Includes messages that were subsequently unsent (deleted), which are otherwise excluded by
    /// default.
    /// </summary>
    IMessageQuery IncludeDeleted();

    /// <summary>
    /// Orders results as specified. Defaults to <see cref="MessageSortOrder.SentAtAscending"/>
    /// when not called.
    /// </summary>
    IMessageQuery OrderBy(MessageSortOrder order);

    /// <summary>Skips the given number of results.</summary>
    IMessageQuery Skip(int count);

    /// <summary>Limits the results to the given number of messages.</summary>
    IMessageQuery Take(int count);

    /// <summary>Executes the query and returns all matching messages as a list.</summary>
    Task<IReadOnlyList<Message>> ToListAsync(CancellationToken cancellationToken = default);

    /// <summary>Executes the query and returns the first matching message, or <see langword="null"/> if none match.</summary>
    Task<Message?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>Executes the query and returns the number of matching messages.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Executes the query, streaming matching messages as they are read.</summary>
    IAsyncEnumerable<Message> AsAsyncEnumerable(CancellationToken cancellationToken = default);
}
