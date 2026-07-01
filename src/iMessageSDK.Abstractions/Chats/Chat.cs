using iMessageSDK.Messages;

namespace iMessageSDK.Chats;

/// <summary>
/// A conversation between the local account and one or more other participants.
/// </summary>
public sealed record Chat
{
    /// <summary>The unique, stable identifier of this chat.</summary>
    public required ChatId Id { get; init; }

    /// <summary>Whether this is a one-on-one or group conversation.</summary>
    public required ChatKind Kind { get; init; }

    /// <summary>
    /// The name to display for this chat: a group's custom subject if one was set, or a
    /// best-effort fallback (for example, participant names) otherwise. <see langword="null"/>
    /// when no reasonable name could be determined.
    /// </summary>
    public string? DisplayName { get; init; }

    /// <summary>
    /// The participants in this chat, including the local account (flagged via
    /// <see cref="Participant.IsMe"/>).
    /// </summary>
    public required IReadOnlyList<Participant> Participants { get; init; }

    /// <summary>The most recent message in this chat, if any.</summary>
    public Message? LastMessage { get; init; }

    /// <summary><see langword="true"/> if the local account has archived this chat.</summary>
    public bool IsArchived { get; init; }
}
