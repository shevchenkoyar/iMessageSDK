namespace iMessageSDK.Chats;

/// <summary>
/// A strongly-typed, immutable identifier for a <see cref="Participant"/>.
/// </summary>
/// <remarks>
/// Wraps the participant's normalized handle (a phone number in E.164 form, or an email address).
/// Two <see cref="ParticipantId"/> values are equal if and only if they wrap the same underlying
/// value.
/// </remarks>
/// <param name="Value">The underlying handle value.</param>
public readonly record struct ParticipantId(string Value)
{
    /// <summary>Returns the underlying handle value.</summary>
    public override string ToString() => Value;
}
