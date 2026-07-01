using iMessageSDK.Attachments;
using iMessageSDK.Internal.AttributedBody;
using iMessageSDK.Internal.Chats;
using iMessageSDK.Internal.Database;
using iMessageSDK.Internal.Messages;
using iMessageSDK.Messages;
using Microsoft.Data.Sqlite;

namespace iMessageSDK.Internal.Querying;

/// <summary>
/// Assembles a single, complete domain <see cref="Message"/> from a raw <see cref="MessageRow"/>
/// by loading its reactions, attachments, and reply preview from the same connection.
/// </summary>
internal static class MessageRowAssembler
{
    public static async Task<Message> AssembleAsync(
        SqliteConnection connection,
        ChatDatabaseSchema schema,
        HandleDirectory handleDirectory,
        MessageRow row,
        CancellationToken cancellationToken)
    {
        var sender = handleDirectory.ResolveSender(row.HandleId, row.IsFromMe);
        var reactions = await LoadReactionsAsync(connection, schema, row.Guid, handleDirectory, cancellationToken).ConfigureAwait(false);
        var attachments = await LoadAttachmentsAsync(connection, schema, row.RowId, cancellationToken).ConfigureAwait(false);
        var replyPreview = row.ReplyToGuid is { Length: > 0 } replyGuid
            ? await LoadMessageTextByGuidAsync(connection, replyGuid, cancellationToken).ConfigureAwait(false)
            : null;
        var attributedText = AttributedBodyParser.TryParse(row.AttributedBody);

        return MessageMapper.Map(row, sender, reactions, attachments, replyPreview, attributedText);
    }

    private static async Task<IReadOnlyList<Reaction>> LoadReactionsAsync(
        SqliteConnection connection, ChatDatabaseSchema schema, string targetGuid, HandleDirectory handleDirectory, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
            {MessageRowMapper.BuildSelectColumns(schema)}
            FROM message m
            JOIN chat_message_join cmj ON cmj.message_id = m.ROWID
            JOIN chat c ON c.ROWID = cmj.chat_id
            WHERE m.associated_message_type BETWEEN 2000 AND 3006
              AND (m.associated_message_guid = @guid OR m.associated_message_guid LIKE '%/' || @guid)
            ORDER BY m.date ASC
            """;
        command.Parameters.AddWithValue("@guid", targetGuid);

        var events = new List<Reaction>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = MessageRowMapper.ReadRow(reader);
            if (!MessageRowMapper.IsReactionRow(row))
            {
                continue;
            }

            var sender = handleDirectory.ResolveSender(row.HandleId, row.IsFromMe);
            events.Add(MessageMapper.MapReaction(row, sender));
        }

        return ReactionStateResolver.ResolveCurrentState(events);
    }

    private static async Task<IReadOnlyList<Attachment>> LoadAttachmentsAsync(
        SqliteConnection connection, ChatDatabaseSchema schema, long messageRowId, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
            {AttachmentRowMapper.BuildSelectColumns(schema)}
            FROM attachment a
            JOIN message_attachment_join maj ON maj.attachment_id = a.ROWID
            WHERE maj.message_id = @messageId
            ORDER BY a.ROWID ASC
            """;
        command.Parameters.AddWithValue("@messageId", messageRowId);

        var rows = new List<AttachmentRow>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add(AttachmentRowMapper.ReadRow(reader));
            }
        }

        var attachments = new List<Attachment>(rows.Count);
        foreach (var row in rows)
        {
            attachments.Add(await Attachments.AttachmentMapper.MapAsync(row, cancellationToken).ConfigureAwait(false));
        }

        return Attachments.LivePhotoPairer.Pair(attachments);
    }

    private static async Task<string?> LoadMessageTextByGuidAsync(SqliteConnection connection, string guid, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT text FROM message WHERE guid = @guid LIMIT 1";
        command.Parameters.AddWithValue("@guid", guid);
        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
    }
}
