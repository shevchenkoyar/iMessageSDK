using iMessageSDK.Chats;

namespace iMessageSDK.Internal.Querying;

/// <summary>An immutable snapshot of the filters accumulated by a fluent <see cref="IChatQuery"/> chain.</summary>
internal sealed record ChatQueryCriteria
{
    public ChatKind? Kind { get; init; }

    public ParticipantId? ParticipantId { get; init; }

    public string? DisplayNameContains { get; init; }

    public SortDirection LastActivityDirection { get; init; } = SortDirection.Descending;

    public int? SkipCount { get; init; }

    public int? TakeCount { get; init; }
}
