using Microsoft.Data.Sqlite;

namespace iMessageSDK.Internal.Database;

/// <summary>
/// Builds the SQL projection for <see cref="ChatRow"/> and reads rows back out of a
/// <see cref="SqliteDataReader"/>.
/// </summary>
internal static class ChatRowMapper
{
    /// <summary>
    /// Builds the column list (without the leading <c>SELECT</c> keyword) for projecting chat
    /// rows. Assumes the <c>chat</c> table is aliased <c>c</c> in the surrounding query.
    /// </summary>
    public static string BuildSelectColumns(ChatDatabaseSchema schema) => string.Join(",\n    ",
    [
        "c.ROWID AS RowId",
        "c.guid AS Guid",
        "c.chat_identifier AS ChatIdentifier",
        "c.display_name AS DisplayName",
        schema.ChatHasColumn("room_name") ? "c.room_name AS RoomName" : "NULL AS RoomName",
        "c.is_archived AS IsArchived",
    ]);

    public static ChatRow ReadRow(SqliteDataReader reader) => new()
    {
        RowId = reader.GetInt64(reader.GetOrdinal(nameof(ChatRow.RowId))),
        Guid = reader.GetString(reader.GetOrdinal(nameof(ChatRow.Guid))),
        ChatIdentifier = reader.GetString(reader.GetOrdinal(nameof(ChatRow.ChatIdentifier))),
        DisplayName = reader.GetNullableStringColumn(nameof(ChatRow.DisplayName)),
        RoomName = reader.GetNullableStringColumn(nameof(ChatRow.RoomName)),
        IsArchived = reader.GetBooleanColumn(nameof(ChatRow.IsArchived)),
    };
}
