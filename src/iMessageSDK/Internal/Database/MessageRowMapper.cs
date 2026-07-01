using Microsoft.Data.Sqlite;

namespace iMessageSDK.Internal.Database;

/// <summary>
/// Builds the SQL projection for <see cref="MessageRow"/> and reads rows back out of a
/// <see cref="SqliteDataReader"/>, tolerating columns that do not exist on the current schema.
/// </summary>
internal static class MessageRowMapper
{
    /// <summary>
    /// Builds the column list (without the leading <c>SELECT</c> keyword) for projecting message
    /// rows joined to their chat's GUID. Assumes the <c>message</c> table is aliased <c>m</c> and
    /// the <c>chat</c> table is aliased <c>c</c> in the surrounding query.
    /// </summary>
    public static string BuildSelectColumns(ChatDatabaseSchema schema)
    {
        string Optional(string column, string alias) =>
            schema.MessageHasColumn(column) ? $"m.{column} AS {alias}" : $"NULL AS {alias}";

        return string.Join(",\n    ",
        [
            "m.ROWID AS RowId",
            "m.guid AS Guid",
            "c.guid AS ChatGuid",
            "m.text AS Text",
            "m.handle_id AS HandleId",
            "m.is_from_me AS IsFromMe",
            "m.date AS Date",
            "m.date_read AS DateRead",
            "m.date_delivered AS DateDelivered",
            "m.is_delivered AS IsDelivered",
            "m.is_sent AS IsSent",
            "m.error AS Error",
            "m.service AS Service",
            "m.item_type AS ItemType",
            "m.group_action_type AS GroupActionType",
            "m.group_title AS GroupTitle",
            "m.associated_message_guid AS AssociatedMessageGuid",
            "m.associated_message_type AS AssociatedMessageType",
            Optional("associated_message_emoji", "AssociatedMessageEmoji"),
            Optional("reply_to_guid", "ReplyToGuid"),
            Optional("date_edited", "DateEdited"),
            Optional("date_retracted", "DateRetracted"),
            "m.cache_has_attachments AS CacheHasAttachments",
            Optional("attributedBody", "AttributedBody"),
            Optional("message_summary_info", "MessageSummaryInfo"),
        ]);
    }

    public static MessageRow ReadRow(SqliteDataReader reader)
    {
        return new MessageRow
        {
            RowId = reader.GetInt64(reader.GetOrdinal(nameof(MessageRow.RowId))),
            Guid = reader.GetString(reader.GetOrdinal(nameof(MessageRow.Guid))),
            ChatGuid = reader.GetString(reader.GetOrdinal(nameof(MessageRow.ChatGuid))),
            Text = reader.GetNullableStringColumn(nameof(MessageRow.Text)),
            HandleId = reader.GetNullableInt64Column(nameof(MessageRow.HandleId)),
            IsFromMe = reader.GetBooleanColumn(nameof(MessageRow.IsFromMe)),
            Date = reader.GetInt64(reader.GetOrdinal(nameof(MessageRow.Date))),
            DateRead = reader.GetNullableInt64Column(nameof(MessageRow.DateRead)),
            DateDelivered = reader.GetNullableInt64Column(nameof(MessageRow.DateDelivered)),
            IsDelivered = reader.GetBooleanColumn(nameof(MessageRow.IsDelivered)),
            IsSent = reader.GetBooleanColumn(nameof(MessageRow.IsSent)),
            Error = reader.GetNullableInt64Column(nameof(MessageRow.Error)) ?? 0,
            Service = reader.GetNullableStringColumn(nameof(MessageRow.Service)),
            ItemType = reader.GetNullableInt64Column(nameof(MessageRow.ItemType)) ?? 0,
            GroupActionType = reader.GetNullableInt64Column(nameof(MessageRow.GroupActionType)),
            GroupTitle = reader.GetNullableStringColumn(nameof(MessageRow.GroupTitle)),
            AssociatedMessageGuid = reader.GetNullableStringColumn(nameof(MessageRow.AssociatedMessageGuid)),
            AssociatedMessageType = reader.GetNullableInt64Column(nameof(MessageRow.AssociatedMessageType)),
            AssociatedMessageEmoji = reader.GetNullableStringColumn(nameof(MessageRow.AssociatedMessageEmoji)),
            ReplyToGuid = reader.GetNullableStringColumn(nameof(MessageRow.ReplyToGuid)),
            DateEdited = reader.GetNullableInt64Column(nameof(MessageRow.DateEdited)),
            DateRetracted = reader.GetNullableInt64Column(nameof(MessageRow.DateRetracted)),
            CacheHasAttachments = reader.GetBooleanColumn(nameof(MessageRow.CacheHasAttachments)),
            AttributedBody = reader.GetNullableBytesColumn(nameof(MessageRow.AttributedBody)),
            MessageSummaryInfo = reader.GetNullableBytesColumn(nameof(MessageRow.MessageSummaryInfo)),
        };
    }

    /// <summary>
    /// <see langword="true"/> when a row represents a tapback/reaction event rather than an
    /// ordinary message.
    /// </summary>
    public static bool IsReactionRow(MessageRow row) =>
        row.AssociatedMessageType is >= 2000 and <= 3006 && row.AssociatedMessageGuid is not null;

    /// <summary>
    /// Strips the "p:&lt;index&gt;/" or "bp:" style prefix Messages sometimes adds to
    /// <see cref="MessageRow.AssociatedMessageGuid"/>, returning the bare target message GUID.
    /// </summary>
    public static string ExtractTargetMessageGuid(string associatedMessageGuid)
    {
        var slashIndex = associatedMessageGuid.IndexOf('/');
        return slashIndex >= 0 ? associatedMessageGuid[(slashIndex + 1)..] : associatedMessageGuid;
    }

}
