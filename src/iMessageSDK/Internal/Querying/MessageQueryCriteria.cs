using iMessageSDK.Chats;
using iMessageSDK.Messages;

namespace iMessageSDK.Internal.Querying;

/// <summary>An immutable snapshot of the filters accumulated by a fluent <see cref="IMessageQuery"/> chain.</summary>
internal sealed record MessageQueryCriteria
{
    public ChatId? ChatId { get; init; }

    public ParticipantId? SenderId { get; init; }

    public string? ContainingText { get; init; }

    public StringComparison ContainingComparison { get; init; } = StringComparison.OrdinalIgnoreCase;

    public DateTimeOffset? After { get; init; }

    public DateTimeOffset? Before { get; init; }

    public MessageKind? Kind { get; init; }

    public bool WithAttachmentsOnly { get; init; }

    public bool IncludeDeletedMessages { get; init; }

    public MessageSortOrder SortOrder { get; init; } = MessageSortOrder.SentAtAscending;

    public int? SkipCount { get; init; }

    public int? TakeCount { get; init; }
}
