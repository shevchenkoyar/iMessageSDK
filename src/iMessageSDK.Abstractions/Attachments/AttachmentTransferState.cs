namespace iMessageSDK.Attachments;

/// <summary>
/// The availability of an attachment's underlying content.
/// </summary>
public enum AttachmentTransferState
{
    /// <summary>The content has not finished transferring (for example, still downloading from iCloud).</summary>
    Pending,

    /// <summary>The content is fully available and can be opened.</summary>
    Downloaded,

    /// <summary>The content is no longer available (for example, it was evicted or never completed transferring).</summary>
    Missing,
}
