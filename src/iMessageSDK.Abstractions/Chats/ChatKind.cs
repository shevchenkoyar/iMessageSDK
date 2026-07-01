namespace iMessageSDK.Chats;

/// <summary>
/// Distinguishes a one-on-one conversation from a group conversation.
/// </summary>
public enum ChatKind
{
    /// <summary>A one-on-one conversation between the local account and a single other participant.</summary>
    Direct,

    /// <summary>A conversation with two or more other participants.</summary>
    Group,
}
