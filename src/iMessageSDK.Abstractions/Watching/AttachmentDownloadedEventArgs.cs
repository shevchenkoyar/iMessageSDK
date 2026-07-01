using iMessageSDK.Attachments;
using iMessageSDK.Messages;

namespace iMessageSDK.Watching;

/// <summary>Event data for <see cref="IMessageWatcher.AttachmentDownloaded"/>.</summary>
public sealed class AttachmentDownloadedEventArgs : EventArgs
{
    /// <summary>The attachment that finished downloading and is now available to read.</summary>
    public required Attachment Attachment { get; init; }

    /// <summary>The identifier of the message the attachment belongs to.</summary>
    public required MessageId MessageId { get; init; }
}
