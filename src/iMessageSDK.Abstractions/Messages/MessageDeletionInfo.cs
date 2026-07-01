namespace iMessageSDK.Messages;

/// <summary>Describes a message that was unsent (deleted) after being sent.</summary>
/// <param name="DeletedAt">The moment the message was unsent.</param>
public sealed record MessageDeletionInfo(DateTimeOffset DeletedAt);
