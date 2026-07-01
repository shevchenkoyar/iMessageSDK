using iMessageSDK.Chats;
using iMessageSDK.Messages;

namespace iMessageSDK.Internal.Querying;

/// <summary>An immutable <see cref="IMessageQuery"/> implementation backed by a <see cref="MessageQueryExecutor"/>.</summary>
internal class MessageQuery : IMessageQuery
{
    private readonly MessageQueryExecutor _executor;
    private readonly MessageQueryCriteria _criteria;

    public MessageQuery(MessageQueryExecutor executor, MessageQueryCriteria criteria)
    {
        _executor = executor;
        _criteria = criteria;
    }

    protected MessageQueryCriteria Criteria => _criteria;

    private MessageQuery With(MessageQueryCriteria criteria) => new(_executor, criteria);

    public IMessageQuery WhereChat(ChatId chatId) => With(_criteria with { ChatId = chatId });

    public IMessageQuery WhereChat(Chat chat) => WhereChat(chat.Id);

    public IMessageQuery WhereSender(ParticipantId participantId) => With(_criteria with { SenderId = participantId });

    public IMessageQuery WhereSender(Participant participant) => WhereSender(participant.Id);

    public IMessageQuery WhereSender(string handle) => WhereSender(new ParticipantId(handle));

    public IMessageQuery Containing(string text, StringComparison comparison = StringComparison.OrdinalIgnoreCase) =>
        With(_criteria with { ContainingText = text, ContainingComparison = comparison });

    public IMessageQuery After(DateTimeOffset timestamp) => With(_criteria with { After = timestamp });

    public IMessageQuery Before(DateTimeOffset timestamp) => With(_criteria with { Before = timestamp });

    public IMessageQuery Between(DateTimeOffset from, DateTimeOffset to) => With(_criteria with { After = from, Before = to });

    public IMessageQuery OfKind(MessageKind kind) => With(_criteria with { Kind = kind });

    public IMessageQuery WithAttachments() => With(_criteria with { WithAttachmentsOnly = true });

    public IMessageQuery IncludeDeleted() => With(_criteria with { IncludeDeletedMessages = true });

    public IMessageQuery OrderBy(MessageSortOrder order) => With(_criteria with { SortOrder = order });

    public IMessageQuery Skip(int count) => With(_criteria with { SkipCount = count });

    public IMessageQuery Take(int count) => With(_criteria with { TakeCount = count });

    public Task<IReadOnlyList<Message>> ToListAsync(CancellationToken cancellationToken = default) =>
        _executor.ToListAsync(_criteria, cancellationToken);

    public Task<Message?> FirstOrDefaultAsync(CancellationToken cancellationToken = default) =>
        _executor.FirstOrDefaultAsync(_criteria, cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        _executor.CountAsync(_criteria, cancellationToken);

    public IAsyncEnumerable<Message> AsAsyncEnumerable(CancellationToken cancellationToken = default) =>
        _executor.AsAsyncEnumerable(_criteria, cancellationToken);
}
