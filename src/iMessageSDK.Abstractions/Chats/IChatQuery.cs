namespace iMessageSDK.Chats;

/// <summary>
/// A composable, immutable query over chats. Each filtering or ordering method returns a new
/// <see cref="IChatQuery"/> reflecting the added criterion; the original instance is unaffected,
/// so queries can be freely branched and reused.
/// </summary>
/// <remarks>
/// Terminal methods (<see cref="ToListAsync"/>, <see cref="FirstOrDefaultAsync"/>,
/// <see cref="CountAsync"/>, <see cref="AsAsyncEnumerable"/>) execute the query. Nothing runs
/// until one of them is called.
/// </remarks>
public interface IChatQuery
{
    /// <summary>Restricts results to chats of the given kind.</summary>
    IChatQuery WhereKind(ChatKind kind);

    /// <summary>Restricts results to chats that include the given participant.</summary>
    IChatQuery WhereParticipant(ParticipantId participantId);

    /// <summary>Restricts results to chats whose display name contains the given substring.</summary>
    IChatQuery WhereDisplayNameContains(string text);

    /// <summary>
    /// Orders results by the timestamp of each chat's most recent message. Defaults to
    /// <see cref="SortDirection.Descending"/> (most recently active first) when not called.
    /// </summary>
    IChatQuery OrderByLastActivity(SortDirection direction = SortDirection.Descending);

    /// <summary>Skips the given number of results.</summary>
    IChatQuery Skip(int count);

    /// <summary>Limits the results to the given number of chats.</summary>
    IChatQuery Take(int count);

    /// <summary>Executes the query and returns all matching chats as a list.</summary>
    Task<IReadOnlyList<Chat>> ToListAsync(CancellationToken cancellationToken = default);

    /// <summary>Executes the query and returns the first matching chat, or <see langword="null"/> if none match.</summary>
    Task<Chat?> FirstOrDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>Executes the query and returns the number of matching chats.</summary>
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>Executes the query, streaming matching chats as they are read.</summary>
    IAsyncEnumerable<Chat> AsAsyncEnumerable(CancellationToken cancellationToken = default);
}
