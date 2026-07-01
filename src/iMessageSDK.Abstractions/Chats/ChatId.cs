namespace iMessageSDK.Chats;

/// <summary>
/// A strongly-typed, immutable identifier for a <see cref="Chat"/>.
/// </summary>
/// <remarks>
/// Wraps the stable identifier Messages assigns to a chat, not any storage-specific row number.
/// Two <see cref="ChatId"/> values are equal if and only if they wrap the same underlying value.
/// </remarks>
/// <param name="Value">The underlying identifier value.</param>
public readonly record struct ChatId(string Value)
{
    /// <summary>Returns the underlying identifier value.</summary>
    public override string ToString() => Value;
}
