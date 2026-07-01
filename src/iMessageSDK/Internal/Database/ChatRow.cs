namespace iMessageSDK.Internal.Database;

/// <summary>A near-verbatim projection of one row of the <c>chat</c> table.</summary>
internal sealed record ChatRow
{
    public required long RowId { get; init; }

    public required string Guid { get; init; }

    public required string ChatIdentifier { get; init; }

    public string? DisplayName { get; init; }

    public string? RoomName { get; init; }

    public bool IsArchived { get; init; }
}
