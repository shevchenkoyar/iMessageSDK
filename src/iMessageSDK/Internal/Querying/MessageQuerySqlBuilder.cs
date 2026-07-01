using iMessageSDK.Internal.Database;
using iMessageSDK.Messages;
using Microsoft.Data.Sqlite;

namespace iMessageSDK.Internal.Querying;

/// <summary>
/// Translates a <see cref="MessageQueryCriteria"/> into a parameterized SQL <c>WHERE</c> clause.
/// Every value supplied by a caller is bound as a parameter; no criterion is ever concatenated
/// into the command text.
/// </summary>
internal static class MessageQuerySqlBuilder
{
    public static (string WhereSql, IReadOnlyList<SqliteParameter> Parameters, bool NeedsHandleJoin) Build(
        MessageQueryCriteria criteria, ChatDatabaseSchema schema)
    {
        var clauses = new List<string>
        {
            "(m.associated_message_type IS NULL OR m.associated_message_type NOT BETWEEN 2000 AND 3006)",
        };
        var parameters = new List<SqliteParameter>();
        var needsHandleJoin = false;

        if (criteria.ChatId is { } chatId)
        {
            clauses.Add("c.guid = @chatGuid");
            parameters.Add(new SqliteParameter("@chatGuid", chatId.Value));
        }

        if (criteria.SenderId is { } senderId)
        {
            if (senderId.Value == "me")
            {
                clauses.Add("m.is_from_me = 1");
            }
            else
            {
                needsHandleJoin = true;
                clauses.Add("m.is_from_me = 0 AND h.id = @senderHandle");
                parameters.Add(new SqliteParameter("@senderHandle", senderId.Value));
            }
        }

        if (criteria.After is { } after)
        {
            clauses.Add("m.date >= @after");
            parameters.Add(new SqliteParameter("@after", AppleTimeConverter.ToAppleNanoseconds(after)));
        }

        if (criteria.Before is { } before)
        {
            clauses.Add("m.date <= @before");
            parameters.Add(new SqliteParameter("@before", AppleTimeConverter.ToAppleNanoseconds(before)));
        }

        if (criteria.Kind is { } kind)
        {
            clauses.Add(kind switch
            {
                MessageKind.GroupAction => "m.item_type != 0",
                MessageKind.Attachment => "m.item_type = 0 AND m.cache_has_attachments = 1 AND (m.text IS NULL OR m.text = '')",
                _ => "m.item_type = 0 AND NOT (m.cache_has_attachments = 1 AND (m.text IS NULL OR m.text = ''))",
            });
        }

        if (criteria.WithAttachmentsOnly)
        {
            clauses.Add("m.cache_has_attachments = 1");
        }

        if (!criteria.IncludeDeletedMessages && schema.MessageHasColumn("date_retracted"))
        {
            clauses.Add("m.date_retracted IS NULL");
        }

        return (string.Join(" AND ", clauses), parameters, needsHandleJoin);
    }
}
