namespace iMessageSDK.Messages;

/// <summary>
/// The chronological order in which an <see cref="IMessageQuery"/> should return results.
/// </summary>
public enum MessageSortOrder
{
    /// <summary>Oldest messages first (chronological reading order). This is the default.</summary>
    SentAtAscending,

    /// <summary>Newest messages first.</summary>
    SentAtDescending,
}
