using iMessageSDK.Chats;
using iMessageSDK.Internal.Database;
using iMessageSDK.Messages;

namespace iMessageSDK.Internal.Chats;

/// <summary>
/// Maps a raw <see cref="ChatRow"/>, together with its already-resolved participants and last
/// message, to the domain <see cref="Chat"/> type.
/// </summary>
internal static class ChatMapper
{
    public static Chat Map(ChatRow row, IReadOnlyList<Participant> participants, Message? lastMessage)
    {
        var otherParticipantCount = participants.Count(p => !p.IsMe);

        return new Chat
        {
            Id = new ChatId(row.Guid),
            Kind = otherParticipantCount >= 2 ? ChatKind.Group : ChatKind.Direct,
            DisplayName = ResolveDisplayName(row, participants),
            Participants = participants,
            LastMessage = lastMessage,
            IsArchived = row.IsArchived,
        };
    }

    private static string? ResolveDisplayName(ChatRow row, IReadOnlyList<Participant> participants)
    {
        if (!string.IsNullOrWhiteSpace(row.DisplayName))
        {
            return row.DisplayName;
        }

        var others = participants.Where(p => !p.IsMe).ToList();
        return others.Count == 0 ? null : string.Join(", ", others.Select(p => p.DisplayName ?? p.Id.Value));
    }
}
