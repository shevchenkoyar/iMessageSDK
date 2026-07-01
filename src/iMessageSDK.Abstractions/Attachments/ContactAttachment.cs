namespace iMessageSDK.Attachments;

/// <summary>
/// An <see cref="Attachment"/> that shares a contact card.
/// </summary>
public sealed record ContactAttachment : Attachment
{
    /// <summary>The shared contact's details.</summary>
    public required ContactCard Contact { get; init; }
}
