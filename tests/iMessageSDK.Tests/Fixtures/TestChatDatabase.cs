using iMessageSDK.Internal.Database;
using Microsoft.Data.Sqlite;

namespace iMessageSDK.Tests.Fixtures;

/// <summary>
/// Builds a temporary SQLite file with a schema subset matching real chat.db, seeded with
/// representative rows, so query and mapping logic can be exercised without the real Messages
/// application or Full Disk Access.
/// </summary>
public sealed class TestChatDatabase : IAsyncDisposable
{
    public static readonly DateTimeOffset BaseTime = new(2024, 1, 1, 10, 0, 0, TimeSpan.Zero);

    public string Path { get; }

    public string AliceHandle { get; } = "+15550001111";

    public string BobHandle { get; } = "+15550002222";

    public string CarolHandle { get; } = "+15550003333";

    public string DirectChatGuid { get; } = "iMessage;-;+15550001111";

    public string GroupChatGuid { get; } = "iMessage;+;chat9988776655";

    public string HelloMessageGuid { get; } = "MSG-1-HELLO";

    public string ReplySourceMessageGuid { get; } = "MSG-9-REPLY";

    public string GroupReplyTargetGuid { get; } = "MSG-5-GROUP-REPLY-TARGET";

    public string EditedMessageGuid { get; } = "MSG-7-EDITED";

    public string DeletedMessageGuid { get; } = "MSG-8-DELETED";

    public string AttachmentGuid { get; } = "ATT-1-IMAGE";

    public string DownloadedAttachmentFilePath { get; }

    private TestChatDatabase(string path, string downloadedAttachmentFilePath)
    {
        Path = path;
        DownloadedAttachmentFilePath = downloadedAttachmentFilePath;
    }

    public static async Task<TestChatDatabase> CreateAsync()
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"imessagesdk-tests-{Guid.NewGuid():N}.db");
        var attachmentPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"imessagesdk-tests-attachment-{Guid.NewGuid():N}.jpg");
        await File.WriteAllBytesAsync(attachmentPath, [0xFF, 0xD8, 0xFF, 0xE0]);

        var database = new TestChatDatabase(path, attachmentPath);
        await database.InitializeAsync().ConfigureAwait(false);
        return database;
    }

    public ValueTask DisposeAsync()
    {
        if (File.Exists(Path))
        {
            File.Delete(Path);
        }

        if (File.Exists(DownloadedAttachmentFilePath))
        {
            File.Delete(DownloadedAttachmentFilePath);
        }

        return ValueTask.CompletedTask;
    }

    private async Task InitializeAsync()
    {
        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path }.ToString());
        await connection.OpenAsync().ConfigureAwait(false);

        await ExecuteAsync(connection, """
            CREATE TABLE message (
                ROWID INTEGER PRIMARY KEY AUTOINCREMENT,
                guid TEXT UNIQUE,
                text TEXT,
                handle_id INTEGER,
                is_from_me INTEGER,
                date INTEGER,
                date_read INTEGER,
                date_delivered INTEGER,
                is_delivered INTEGER,
                is_sent INTEGER,
                error INTEGER,
                service TEXT,
                item_type INTEGER,
                group_action_type INTEGER,
                group_title TEXT,
                associated_message_guid TEXT,
                associated_message_type INTEGER,
                associated_message_emoji TEXT,
                reply_to_guid TEXT,
                date_edited INTEGER,
                date_retracted INTEGER,
                cache_has_attachments INTEGER,
                attributedBody BLOB,
                message_summary_info BLOB
            );

            CREATE TABLE chat (
                ROWID INTEGER PRIMARY KEY AUTOINCREMENT,
                guid TEXT UNIQUE,
                chat_identifier TEXT,
                display_name TEXT,
                room_name TEXT,
                is_archived INTEGER
            );

            CREATE TABLE handle (
                ROWID INTEGER PRIMARY KEY AUTOINCREMENT,
                id TEXT
            );

            CREATE TABLE chat_message_join (chat_id INTEGER, message_id INTEGER);
            CREATE TABLE chat_handle_join (chat_id INTEGER, handle_id INTEGER);

            CREATE TABLE attachment (
                ROWID INTEGER PRIMARY KEY AUTOINCREMENT,
                guid TEXT UNIQUE,
                filename TEXT,
                uti TEXT,
                mime_type TEXT,
                transfer_name TEXT,
                total_bytes INTEGER,
                is_sticker INTEGER,
                created_date INTEGER
            );

            CREATE TABLE message_attachment_join (message_id INTEGER, attachment_id INTEGER);
            """).ConfigureAwait(false);

        var aliceHandleId = await InsertHandleAsync(connection, AliceHandle).ConfigureAwait(false);
        var bobHandleId = await InsertHandleAsync(connection, BobHandle).ConfigureAwait(false);
        var carolHandleId = await InsertHandleAsync(connection, CarolHandle).ConfigureAwait(false);

        var directChatId = await InsertChatAsync(connection, DirectChatGuid, AliceHandle, null, null).ConfigureAwait(false);
        var groupChatId = await InsertChatAsync(connection, GroupChatGuid, "chat9988776655", "Trip Planning", "chat9988776655").ConfigureAwait(false);

        await LinkChatHandleAsync(connection, directChatId, aliceHandleId).ConfigureAwait(false);
        await LinkChatHandleAsync(connection, groupChatId, bobHandleId).ConfigureAwait(false);
        await LinkChatHandleAsync(connection, groupChatId, carolHandleId).ConfigureAwait(false);

        // A plain incoming message.
        var helloMessageId = await InsertMessageAsync(connection, directChatId, HelloMessageGuid, "Hello there",
            handleId: aliceHandleId, isFromMe: false, sentAt: BaseTime).ConfigureAwait(false);

        // A read outgoing reply.
        await InsertMessageAsync(connection, directChatId, "MSG-2-HI-ALICE", "Hi Alice!",
            handleId: null, isFromMe: true, sentAt: BaseTime.AddMinutes(1),
            dateDelivered: BaseTime.AddMinutes(2), dateRead: BaseTime.AddMinutes(3)).ConfigureAwait(false);

        // An incoming message with an attachment (no text).
        var attachmentMessageId = await InsertMessageAsync(connection, directChatId, "MSG-3-PHOTO", null,
            handleId: aliceHandleId, isFromMe: false, sentAt: BaseTime.AddMinutes(4), cacheHasAttachments: true).ConfigureAwait(false);
        var attachmentId = await InsertAttachmentAsync(connection, AttachmentGuid, DownloadedAttachmentFilePath, "public.jpeg", "image/jpeg").ConfigureAwait(false);
        await LinkMessageAttachmentAsync(connection, attachmentMessageId, attachmentId).ConfigureAwait(false);

        // A group message that later receives a "Loved" tapback from Carol.
        await InsertMessageAsync(connection, groupChatId, "MSG-4-BEACH", "Let's go to the beach",
            handleId: bobHandleId, isFromMe: false, sentAt: BaseTime.AddHours(1)).ConfigureAwait(false);
        var groupReplyTargetId = await InsertMessageAsync(connection, groupChatId, GroupReplyTargetGuid, "Sounds great",
            handleId: null, isFromMe: true, sentAt: BaseTime.AddHours(1).AddMinutes(1)).ConfigureAwait(false);
        await InsertReactionAsync(connection, groupChatId, "MSG-6-REACTION", GroupReplyTargetGuid,
            associatedMessageType: 2000, handleId: carolHandleId, reactedAt: BaseTime.AddHours(1).AddMinutes(2)).ConfigureAwait(false);

        // An edited message.
        await InsertMessageAsync(connection, directChatId, EditedMessageGuid, "This got edited",
            handleId: aliceHandleId, isFromMe: false, sentAt: BaseTime.AddHours(2),
            dateEdited: BaseTime.AddHours(2).AddMinutes(5)).ConfigureAwait(false);

        // A deleted (unsent) message.
        await InsertMessageAsync(connection, directChatId, DeletedMessageGuid, "oops deleting this",
            handleId: null, isFromMe: true, sentAt: BaseTime.AddHours(3),
            dateRetracted: BaseTime.AddHours(3).AddSeconds(10)).ConfigureAwait(false);

        // A reply to the first message.
        await InsertMessageAsync(connection, directChatId, ReplySourceMessageGuid, "Replying to hello",
            handleId: aliceHandleId, isFromMe: false, sentAt: BaseTime.AddHours(4), replyToGuid: HelloMessageGuid).ConfigureAwait(false);

        // A group action (participant added).
        await InsertMessageAsync(connection, groupChatId, "MSG-10-GROUP-ACTION", null,
            handleId: bobHandleId, isFromMe: false, sentAt: BaseTime.AddHours(5), itemType: 1, groupActionType: 0).ConfigureAwait(false);

        _ = helloMessageId;
        _ = attachmentMessageId;
    }

    private static async Task ExecuteAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static async Task<long> InsertHandleAsync(SqliteConnection connection, string handle)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO handle (id) VALUES (@id); SELECT last_insert_rowid();";
        command.Parameters.AddWithValue("@id", handle);
        return (long)(await command.ExecuteScalarAsync().ConfigureAwait(false))!;
    }

    private static async Task<long> InsertChatAsync(SqliteConnection connection, string guid, string chatIdentifier, string? displayName, string? roomName)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO chat (guid, chat_identifier, display_name, room_name, is_archived)
            VALUES (@guid, @chatIdentifier, @displayName, @roomName, 0);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("@guid", guid);
        command.Parameters.AddWithValue("@chatIdentifier", chatIdentifier);
        command.Parameters.AddWithValue("@displayName", (object?)displayName ?? DBNull.Value);
        command.Parameters.AddWithValue("@roomName", (object?)roomName ?? DBNull.Value);
        return (long)(await command.ExecuteScalarAsync().ConfigureAwait(false))!;
    }

    private static async Task LinkChatHandleAsync(SqliteConnection connection, long chatId, long handleId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO chat_handle_join (chat_id, handle_id) VALUES (@chatId, @handleId)";
        command.Parameters.AddWithValue("@chatId", chatId);
        command.Parameters.AddWithValue("@handleId", handleId);
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static async Task<long> InsertMessageAsync(
        SqliteConnection connection,
        long chatId,
        string guid,
        string? text,
        long? handleId,
        bool isFromMe,
        DateTimeOffset sentAt,
        DateTimeOffset? dateDelivered = null,
        DateTimeOffset? dateRead = null,
        DateTimeOffset? dateEdited = null,
        DateTimeOffset? dateRetracted = null,
        string? replyToGuid = null,
        bool cacheHasAttachments = false,
        long itemType = 0,
        long? groupActionType = null)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO message (
                guid, text, handle_id, is_from_me, date, date_read, date_delivered, is_delivered,
                is_sent, error, service, item_type, group_action_type, group_title,
                associated_message_guid, associated_message_type, associated_message_emoji,
                reply_to_guid, date_edited, date_retracted, cache_has_attachments)
            VALUES (
                @guid, @text, @handleId, @isFromMe, @date, @dateRead, @dateDelivered, @isDelivered,
                @isSent, 0, 'iMessage', @itemType, @groupActionType, NULL,
                NULL, NULL, NULL,
                @replyToGuid, @dateEdited, @dateRetracted, @cacheHasAttachments);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("@guid", guid);
        command.Parameters.AddWithValue("@text", (object?)text ?? DBNull.Value);
        command.Parameters.AddWithValue("@handleId", (object?)handleId ?? DBNull.Value);
        command.Parameters.AddWithValue("@isFromMe", isFromMe ? 1 : 0);
        command.Parameters.AddWithValue("@date", AppleTimeConverter.ToAppleNanoseconds(sentAt));
        command.Parameters.AddWithValue("@dateRead", dateRead is null ? DBNull.Value : AppleTimeConverter.ToAppleNanoseconds(dateRead.Value));
        command.Parameters.AddWithValue("@dateDelivered", dateDelivered is null ? DBNull.Value : AppleTimeConverter.ToAppleNanoseconds(dateDelivered.Value));
        command.Parameters.AddWithValue("@isDelivered", dateDelivered is null ? 0 : 1);
        command.Parameters.AddWithValue("@isSent", isFromMe ? 1 : 0);
        command.Parameters.AddWithValue("@itemType", itemType);
        command.Parameters.AddWithValue("@groupActionType", (object?)groupActionType ?? DBNull.Value);
        command.Parameters.AddWithValue("@replyToGuid", (object?)replyToGuid ?? DBNull.Value);
        command.Parameters.AddWithValue("@dateEdited", dateEdited is null ? DBNull.Value : AppleTimeConverter.ToAppleNanoseconds(dateEdited.Value));
        command.Parameters.AddWithValue("@dateRetracted", dateRetracted is null ? DBNull.Value : AppleTimeConverter.ToAppleNanoseconds(dateRetracted.Value));
        command.Parameters.AddWithValue("@cacheHasAttachments", cacheHasAttachments ? 1 : 0);

        var messageId = (long)(await command.ExecuteScalarAsync().ConfigureAwait(false))!;

        await using var joinCommand = connection.CreateCommand();
        joinCommand.CommandText = "INSERT INTO chat_message_join (chat_id, message_id) VALUES (@chatId, @messageId)";
        joinCommand.Parameters.AddWithValue("@chatId", chatId);
        joinCommand.Parameters.AddWithValue("@messageId", messageId);
        await joinCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

        return messageId;
    }

    private static async Task InsertReactionAsync(
        SqliteConnection connection, long chatId, string guid, string targetMessageGuid, long associatedMessageType, long handleId, DateTimeOffset reactedAt)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO message (
                guid, text, handle_id, is_from_me, date, is_delivered, is_sent, error, service,
                item_type, associated_message_guid, associated_message_type, cache_has_attachments)
            VALUES (
                @guid, NULL, @handleId, 0, @date, 1, 0, 0, 'iMessage',
                0, @targetGuid, @associatedType, 0);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("@guid", guid);
        command.Parameters.AddWithValue("@handleId", handleId);
        command.Parameters.AddWithValue("@date", AppleTimeConverter.ToAppleNanoseconds(reactedAt));
        command.Parameters.AddWithValue("@targetGuid", targetMessageGuid);
        command.Parameters.AddWithValue("@associatedType", associatedMessageType);

        var messageId = (long)(await command.ExecuteScalarAsync().ConfigureAwait(false))!;

        await using var joinCommand = connection.CreateCommand();
        joinCommand.CommandText = "INSERT INTO chat_message_join (chat_id, message_id) VALUES (@chatId, @messageId)";
        joinCommand.Parameters.AddWithValue("@chatId", chatId);
        joinCommand.Parameters.AddWithValue("@messageId", messageId);
        await joinCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    private static async Task<long> InsertAttachmentAsync(SqliteConnection connection, string guid, string filePath, string uti, string mimeType)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO attachment (guid, filename, uti, mime_type, transfer_name, total_bytes, is_sticker, created_date)
            VALUES (@guid, @filename, @uti, @mimeType, @transferName, @totalBytes, 0, @createdDate);
            SELECT last_insert_rowid();
            """;
        command.Parameters.AddWithValue("@guid", guid);
        command.Parameters.AddWithValue("@filename", filePath);
        command.Parameters.AddWithValue("@uti", uti);
        command.Parameters.AddWithValue("@mimeType", mimeType);
        command.Parameters.AddWithValue("@transferName", System.IO.Path.GetFileName(filePath));
        command.Parameters.AddWithValue("@totalBytes", 4L);
        command.Parameters.AddWithValue("@createdDate", AppleTimeConverter.ToAppleNanoseconds(BaseTime));
        return (long)(await command.ExecuteScalarAsync().ConfigureAwait(false))!;
    }

    private static async Task LinkMessageAttachmentAsync(SqliteConnection connection, long messageId, long attachmentId)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO message_attachment_join (message_id, attachment_id) VALUES (@messageId, @attachmentId)";
        command.Parameters.AddWithValue("@messageId", messageId);
        command.Parameters.AddWithValue("@attachmentId", attachmentId);
        await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }
}
