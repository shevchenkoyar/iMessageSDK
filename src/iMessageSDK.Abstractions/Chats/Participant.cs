namespace iMessageSDK.Chats;

/// <summary>
/// A person taking part in one or more chats.
/// </summary>
public sealed record Participant
{
    /// <summary>The participant's normalized handle (phone number or email address).</summary>
    public required ParticipantId Id { get; init; }

    /// <summary>
    /// The participant's display name, if known from the local address book. <see langword="null"/>
    /// when no contact match was found, in which case <see cref="Id"/> is the best available label.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// <see langword="true"/> if this participant represents the local account (the SDK's own
    /// user) rather than another person.
    /// </summary>
    public bool IsMe { get; init; }
}
