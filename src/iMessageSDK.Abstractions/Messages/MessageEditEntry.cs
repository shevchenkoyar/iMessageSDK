namespace iMessageSDK.Messages;

/// <summary>A single historical revision of an edited message's text.</summary>
/// <param name="Text">The message text as it read after this revision.</param>
/// <param name="EditedAt">The moment this revision was made.</param>
public sealed record MessageEditEntry(string Text, DateTimeOffset EditedAt);
