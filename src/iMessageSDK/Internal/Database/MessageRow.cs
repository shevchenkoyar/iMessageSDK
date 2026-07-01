namespace iMessageSDK.Internal.Database;

/// <summary>
/// A near-verbatim projection of one row of the <c>message</c> table (joined to its owning chat's
/// GUID), before any domain interpretation is applied.
/// </summary>
/// <remarks>
/// A single database row can represent either an ordinary message or a tapback/reaction "event"
/// attached to another message (see <see cref="AssociatedMessageType"/>); the query engine's
/// mapping layer is responsible for telling the two apart and assembling the final domain types.
/// </remarks>
internal sealed record MessageRow
{
    public required long RowId { get; init; }

    public required string Guid { get; init; }

    public required string ChatGuid { get; init; }

    public string? Text { get; init; }

    public long? HandleId { get; init; }

    public required bool IsFromMe { get; init; }

    public required long Date { get; init; }

    public long? DateRead { get; init; }

    public long? DateDelivered { get; init; }

    public bool IsDelivered { get; init; }

    public bool IsSent { get; init; }

    public long Error { get; init; }

    public string? Service { get; init; }

    public long ItemType { get; init; }

    public long? GroupActionType { get; init; }

    public string? GroupTitle { get; init; }

    public string? AssociatedMessageGuid { get; init; }

    public long? AssociatedMessageType { get; init; }

    public string? AssociatedMessageEmoji { get; init; }

    public string? ReplyToGuid { get; init; }

    public long? DateEdited { get; init; }

    public long? DateRetracted { get; init; }

    public bool CacheHasAttachments { get; init; }

    public byte[]? AttributedBody { get; init; }

    public byte[]? MessageSummaryInfo { get; init; }
}
