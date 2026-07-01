namespace iMessageSDK.Attachments;

/// <summary>
/// An <see cref="Attachment"/> representing an Apple Live Photo: a still image paired with a
/// short companion motion video.
/// </summary>
/// <remarks>
/// This attachment's own identity (<see cref="Attachment.Id"/>) refers to the still image;
/// <see cref="MotionVideoAttachmentId"/> refers to the companion video, which can be opened
/// independently through <c>IAttachmentsModule.OpenReadAsync</c>.
/// </remarks>
public sealed record LivePhotoAttachment : Attachment
{
    /// <summary>The identifier of the companion motion video attachment.</summary>
    public required AttachmentId MotionVideoAttachmentId { get; init; }
}
