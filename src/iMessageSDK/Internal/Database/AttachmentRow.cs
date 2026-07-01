namespace iMessageSDK.Internal.Database;

/// <summary>A near-verbatim projection of one row of the <c>attachment</c> table.</summary>
internal sealed record AttachmentRow
{
    public required long RowId { get; init; }

    public required string Guid { get; init; }

    public string? FileName { get; init; }

    public string? UniformTypeIdentifier { get; init; }

    public string? MimeType { get; init; }

    public string? TransferName { get; init; }

    public long? TotalBytes { get; init; }

    public bool IsSticker { get; init; }

    public long? CreatedDate { get; init; }
}
