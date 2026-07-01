using iMessageSDK.Chats;

namespace iMessageSDK.Internal.Querying;

/// <summary>An immutable <see cref="IChatQuery"/> implementation backed by a <see cref="ChatQueryExecutor"/>.</summary>
internal class ChatQuery : IChatQuery
{
    private readonly ChatQueryExecutor _executor;
    private readonly ChatQueryCriteria _criteria;

    public ChatQuery(ChatQueryExecutor executor, ChatQueryCriteria criteria)
    {
        _executor = executor;
        _criteria = criteria;
    }

    protected ChatQueryCriteria Criteria => _criteria;

    private ChatQuery With(ChatQueryCriteria criteria) => new(_executor, criteria);

    public IChatQuery WhereKind(ChatKind kind) => With(_criteria with { Kind = kind });

    public IChatQuery WhereParticipant(ParticipantId participantId) => With(_criteria with { ParticipantId = participantId });

    public IChatQuery WhereDisplayNameContains(string text) => With(_criteria with { DisplayNameContains = text });

    public IChatQuery OrderByLastActivity(SortDirection direction = SortDirection.Descending) =>
        With(_criteria with { LastActivityDirection = direction });

    public IChatQuery Skip(int count) => With(_criteria with { SkipCount = count });

    public IChatQuery Take(int count) => With(_criteria with { TakeCount = count });

    public Task<IReadOnlyList<Chat>> ToListAsync(CancellationToken cancellationToken = default) =>
        _executor.ToListAsync(_criteria, cancellationToken);

    public Task<Chat?> FirstOrDefaultAsync(CancellationToken cancellationToken = default) =>
        _executor.FirstOrDefaultAsync(_criteria, cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _executor.CountAsync(_criteria, cancellationToken);

    public IAsyncEnumerable<Chat> AsAsyncEnumerable(CancellationToken cancellationToken = default) =>
        _executor.AsAsyncEnumerable(_criteria, cancellationToken);
}
