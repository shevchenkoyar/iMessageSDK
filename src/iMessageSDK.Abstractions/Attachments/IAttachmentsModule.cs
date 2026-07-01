namespace iMessageSDK.Attachments;

/// <summary>
/// The entry point for retrieving attachment metadata and opening attachment content.
/// </summary>
/// <remarks>
/// This interface is intended to be consumed, not implemented, by SDK users; new members may be
/// added in future minor versions without that being considered a breaking change.
/// </remarks>
public interface IAttachmentsModule
{
    /// <summary>Retrieves a single attachment's metadata by its identifier, or <see langword="null"/> if it does not exist.</summary>
    Task<Attachment?> GetAsync(AttachmentId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a readable stream over the attachment's underlying content.
    /// </summary>
    /// <param name="id">The identifier of the attachment to open.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="Exceptions.AttachmentNotAvailableException">
    /// The attachment's content is not currently available (see <see cref="Attachment.TransferState"/>).
    /// </exception>
    Task<Stream> OpenReadAsync(AttachmentId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a readable stream over the attachment's underlying content.
    /// </summary>
    /// <param name="attachment">The attachment to open.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <exception cref="Exceptions.AttachmentNotAvailableException">
    /// The attachment's content is not currently available (see <see cref="Attachment.TransferState"/>).
    /// </exception>
    Task<Stream> OpenReadAsync(Attachment attachment, CancellationToken cancellationToken = default);
}
