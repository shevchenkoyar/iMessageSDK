namespace iMessageSDK.Attachments;

/// <summary>
/// An <see cref="Attachment"/> that shares a geographic location.
/// </summary>
public sealed record LocationAttachment : Attachment
{
    /// <summary>The shared coordinate.</summary>
    public required GeoCoordinate Coordinate { get; init; }

    /// <summary>A human-readable label for the location, if provided.</summary>
    public string? Label { get; init; }
}
