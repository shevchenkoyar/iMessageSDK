using iMessageSDK.Attachments;
using iMessageSDK.Chats;
using iMessageSDK.Internal.Database;
using iMessageSDK.Messages;

namespace iMessageSDK.Internal.Messages;

/// <summary>
/// Assembles a domain <see cref="Message"/> from a raw <see cref="MessageRow"/> together with its
/// already-resolved sender, reactions, attachments, reply preview, and rich text. Orchestrating
/// those auxiliary lookups is the query engine's responsibility; this type is a pure function of
/// its inputs.
/// </summary>
internal static class MessageMapper
{
    public static Message Map(
        MessageRow row,
        Participant? sender,
        IReadOnlyList<Reaction> reactions,
        IReadOnlyList<Attachment> attachments,
        string? replyPreviewText,
        AttributedText? attributedText)
    {
        var sentAt = AppleTimeConverter.ToDateTimeOffset(row.Date) ?? DateTimeOffset.UnixEpoch;
        var deliveredAt = AppleTimeConverter.ToDateTimeOffset(row.DateDelivered);
        var readAt = AppleTimeConverter.ToDateTimeOffset(row.DateRead);
        var editedAt = AppleTimeConverter.ToDateTimeOffset(row.DateEdited);
        var deletedAt = AppleTimeConverter.ToDateTimeOffset(row.DateRetracted);

        var text = row.Text ?? attributedText?.PlainText;

        return new Message
        {
            Id = new MessageId(row.Guid),
            ChatId = new ChatId(row.ChatGuid),
            Kind = DetermineKind(row, attachments),
            Sender = sender,
            IsFromMe = row.IsFromMe,
            Text = text,
            AttributedText = attributedText,
            Attachments = attachments,
            SentAt = sentAt,
            DeliveredAt = deliveredAt,
            ReadAt = readAt,
            Status = DetermineStatus(row, deliveredAt, readAt),
            Service = string.Equals(row.Service, "iMessage", StringComparison.OrdinalIgnoreCase)
                ? DeliveryChannel.IMessage
                : DeliveryChannel.SmsForwarding,
            ReplyTo = row.ReplyToGuid is { Length: > 0 } replyToGuid
                ? new ReplyMetadata { RepliedToMessageId = new MessageId(replyToGuid), RepliedToPreviewText = replyPreviewText }
                : null,
            Reactions = reactions,
            EditInfo = editedAt is { } lastEditedAt
                ? new MessageEditInfo { LastEditedAt = lastEditedAt, History = [new MessageEditEntry(text ?? string.Empty, lastEditedAt)] }
                : null,
            DeletionInfo = deletedAt is { } deletedAtValue ? new MessageDeletionInfo(deletedAtValue) : null,
            GroupActionDescription = row.ItemType != 0 ? BuildGroupActionDescription(row, sender) : null,
        };
    }

    /// <summary>Maps a tapback/reaction row (see <see cref="MessageRowMapper.IsReactionRow"/>) to a <see cref="Reaction"/>.</summary>
    public static Reaction MapReaction(MessageRow reactionRow, Participant sender)
    {
        var associatedType = reactionRow.AssociatedMessageType!.Value;
        var isRemoved = associatedType >= 3000;
        var normalizedType = isRemoved ? associatedType - 1000 : associatedType;

        return new Reaction
        {
            Kind = normalizedType switch
            {
                2000 => TapbackKind.Loved,
                2001 => TapbackKind.Liked,
                2002 => TapbackKind.Disliked,
                2003 => TapbackKind.Laughed,
                2004 => TapbackKind.Emphasized,
                2005 => TapbackKind.Questioned,
                _ => TapbackKind.Emoji,
            },
            EmojiValue = reactionRow.AssociatedMessageEmoji,
            Sender = sender,
            ReactedAt = AppleTimeConverter.ToDateTimeOffset(reactionRow.Date) ?? DateTimeOffset.UnixEpoch,
            IsRemoved = isRemoved,
        };
    }

    private static MessageKind DetermineKind(MessageRow row, IReadOnlyList<Attachment> attachments)
    {
        if (row.ItemType != 0)
        {
            return MessageKind.GroupAction;
        }

        return attachments.Count > 0 && string.IsNullOrEmpty(row.Text) ? MessageKind.Attachment : MessageKind.PlainText;
    }

    private static MessageStatus DetermineStatus(MessageRow row, DateTimeOffset? deliveredAt, DateTimeOffset? readAt)
    {
        if (row.Error != 0)
        {
            return MessageStatus.Failed;
        }

        if (!row.IsFromMe)
        {
            // An incoming row only ever lands in the local database once received.
            return readAt is not null ? MessageStatus.Read : MessageStatus.Delivered;
        }

        if (readAt is not null)
        {
            return MessageStatus.Read;
        }

        if (deliveredAt is not null)
        {
            return MessageStatus.Delivered;
        }

        return row.IsSent ? MessageStatus.Sent : MessageStatus.Pending;
    }

    private static string BuildGroupActionDescription(MessageRow row, Participant? sender)
    {
        var actorName = sender?.DisplayName ?? sender?.Id.Value ?? "Someone";
        return row.GroupActionType switch
        {
            0 => $"{actorName} added a participant to the conversation.",
            1 => $"{actorName} removed a participant from the conversation.",
            _ when !string.IsNullOrEmpty(row.GroupTitle) => $"{actorName} named the conversation \"{row.GroupTitle}\".",
            _ => $"{actorName} updated the conversation.",
        };
    }
}
