namespace iMessageSDK.Attachments;

/// <summary>
/// Content carried by a message, distinct from its text: media, files, stickers, contact cards,
/// locations, or Live Photos.
/// </summary>
/// <remarks>
/// <see cref="Attachment"/> is a closed hierarchy: <see cref="MediaAttachment"/>,
/// <see cref="ContactAttachment"/>, <see cref="LocationAttachment"/>, and
/// <see cref="LivePhotoAttachment"/> are the only derived types, so consumers can safely pattern
/// match with a <c>switch</c> expression. Use <see cref="Messages.IMessagesModule"/>'s
/// <c>IAttachmentsModule.OpenReadAsync</c> to obtain the underlying content stream.
/// </remarks>
public abstract record Attachment
{
    /// <summary>The unique, stable identifier of this attachment.</summary>
    public required AttachmentId Id { get; init; }

    /// <summary>The category of content this attachment carries.</summary>
    public required AttachmentKind Kind { get; init; }

    /// <summary>The original file name, if known.</summary>
    public string? FileName { get; init; }

    /// <summary>The IANA media (MIME) type, if known.</summary>
    public string? MimeType { get; init; }

    /// <summary>The size of the underlying content in bytes, if known.</summary>
    public long? SizeInBytes { get; init; }

    /// <summary>Whether the underlying content is currently available to read.</summary>
    public required AttachmentTransferState TransferState { get; init; }

    /// <summary>
    /// <see langword="true"/> if this attachment was sent as a sticker (optionally overlaid on
    /// another attachment).
    /// </summary>
    public bool IsSticker { get; init; }

    /// <summary>The moment this attachment was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
