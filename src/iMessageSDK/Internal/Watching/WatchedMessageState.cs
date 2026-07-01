using iMessageSDK.Attachments;

namespace iMessageSDK.Internal.Watching;

/// <summary>The last-observed state of a message the watcher has already reported, used to detect edits, deletions, and attachment downloads on subsequent polls.</summary>
internal sealed record WatchedMessageState(
    string? Text,
    long? DateEdited,
    long? DateRetracted,
    IReadOnlyDictionary<AttachmentId, AttachmentTransferState> AttachmentStates);
