using System.Runtime.CompilerServices;
using iMessageSDK.Internal.Chats;
using iMessageSDK.Internal.Database;
using iMessageSDK.Messages;
using Microsoft.Data.Sqlite;

namespace iMessageSDK.Internal.Querying;

/// <summary>
/// Executes <see cref="MessageQueryCriteria"/> against the conversation history file, mapping raw
/// rows to fully-assembled domain <see cref="Message"/> instances.
/// </summary>
/// <remarks>
/// Each top-level call opens its own short-lived read-only connection: the conversation history
/// file supports any number of concurrent readers, and this avoids any shared-connection
/// threading concerns. A page of matching primary rows is buffered in full before per-row
/// reactions/attachments/reply-preview are assembled, so a single call never holds more than one
/// SQL cursor open on its connection at a time.
/// </remarks>
internal sealed class MessageQueryExecutor
{
    private readonly string _databasePath;

    public MessageQueryExecutor(string databasePath)
    {
        _databasePath = databasePath;
    }

    public async Task<IReadOnlyList<Message>> ToListAsync(MessageQueryCriteria criteria, CancellationToken cancellationToken)
    {
        var results = new List<Message>();
        await foreach (var message in AsAsyncEnumerable(criteria, cancellationToken).ConfigureAwait(false))
        {
            results.Add(message);
        }

        return results;
    }

    public async Task<Message?> GetByIdAsync(MessageId id, CancellationToken cancellationToken)
    {
        await using var connection = await ChatDatabaseConnectionFactory.OpenAsync(_databasePath, cancellationToken).ConfigureAwait(false);
        var schema = await ChatDatabaseSchema.LoadAsync(connection, cancellationToken).ConfigureAwait(false);
        var handleDirectory = await HandleDirectory.LoadAsync(connection, cancellationToken).ConfigureAwait(false);

        MessageRow? row;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"""
                SELECT
                {MessageRowMapper.BuildSelectColumns(schema)}
                FROM message m
                JOIN chat_message_join cmj ON cmj.message_id = m.ROWID
                JOIN chat c ON c.ROWID = cmj.chat_id
                WHERE m.guid = @guid
                LIMIT 1
                """;
            command.Parameters.AddWithValue("@guid", id.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            row = await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? MessageRowMapper.ReadRow(reader) : null;
        }

        return row is null
            ? null
            : await MessageRowAssembler.AssembleAsync(connection, schema, handleDirectory, row, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Message?> FirstOrDefaultAsync(MessageQueryCriteria criteria, CancellationToken cancellationToken)
    {
        var narrowed = criteria with { TakeCount = 1 };
        await foreach (var message in AsAsyncEnumerable(narrowed, cancellationToken).ConfigureAwait(false))
        {
            return message;
        }

        return null;
    }

    public async Task<int> CountAsync(MessageQueryCriteria criteria, CancellationToken cancellationToken)
    {
        await using var connection = await ChatDatabaseConnectionFactory.OpenAsync(_databasePath, cancellationToken).ConfigureAwait(false);
        var schema = await ChatDatabaseSchema.LoadAsync(connection, cancellationToken).ConfigureAwait(false);
        var (whereSql, parameters, needsHandleJoin) = MessageQuerySqlBuilder.Build(criteria, schema);

        if (criteria.ContainingText is null)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"""
                SELECT COUNT(*)
                FROM message m
                JOIN chat_message_join cmj ON cmj.message_id = m.ROWID
                JOIN chat c ON c.ROWID = cmj.chat_id
                {(needsHandleJoin ? "LEFT JOIN handle h ON h.ROWID = m.handle_id" : string.Empty)}
                WHERE {whereSql}
                """;
            foreach (var parameter in parameters)
            {
                command.Parameters.Add(parameter);
            }

            var scalar = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return Convert.ToInt32(scalar);
        }

        // Content search requires recovering each row's effective text (including, when the
        // plain `text` column is empty, best-effort attributedBody recovery), which cannot be
        // pushed into SQL, so every candidate row is evaluated in-memory here. Reactions,
        // attachments, and reply previews are not needed just to count, so this is cheaper than
        // ToListAsync despite not being a single SQL aggregate.
        var rows = await FetchPrimaryRowsAsync(connection, schema, criteria, applyPaging: false, cancellationToken).ConfigureAwait(false);
        var count = 0;
        foreach (var row in rows)
        {
            var attributedText = row.AttributedBody is null ? null : Internal.AttributedBody.AttributedBodyParser.TryParse(row.AttributedBody);
            var text = row.Text ?? attributedText?.PlainText;
            if (MatchesContaining(text, criteria.ContainingText, criteria.ContainingComparison))
            {
                count++;
            }
        }

        return count;
    }

    public async IAsyncEnumerable<Message> AsAsyncEnumerable(
        MessageQueryCriteria criteria, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var connection = await ChatDatabaseConnectionFactory.OpenAsync(_databasePath, cancellationToken).ConfigureAwait(false);
        var schema = await ChatDatabaseSchema.LoadAsync(connection, cancellationToken).ConfigureAwait(false);
        var handleDirectory = await HandleDirectory.LoadAsync(connection, cancellationToken).ConfigureAwait(false);

        // A content-search predicate cannot be evaluated in SQL (it may need attributedBody
        // recovery), so pagination in that case is applied after filtering, in-memory, below.
        var pagingAppliedInSql = criteria.ContainingText is null;
        var rows = await FetchPrimaryRowsAsync(connection, schema, criteria, pagingAppliedInSql, cancellationToken).ConfigureAwait(false);

        var skipRemaining = pagingAppliedInSql ? 0 : criteria.SkipCount ?? 0;
        var takeRemaining = pagingAppliedInSql ? int.MaxValue : criteria.TakeCount ?? int.MaxValue;
        var emitted = 0;

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var message = await MessageRowAssembler.AssembleAsync(connection, schema, handleDirectory, row, cancellationToken).ConfigureAwait(false);

            if (criteria.ContainingText is { } needle && !MatchesContaining(message.Text, needle, criteria.ContainingComparison))
            {
                continue;
            }

            if (!pagingAppliedInSql)
            {
                if (skipRemaining > 0)
                {
                    skipRemaining--;
                    continue;
                }

                if (emitted >= takeRemaining)
                {
                    yield break;
                }
            }

            emitted++;
            yield return message;
        }
    }

    /// <summary>
    /// Retrieves the single most recent non-reaction message for a chat, fully assembled. Reuses
    /// an already-open connection so <see cref="ChatQueryExecutor"/> can populate
    /// <c>Chat.LastMessage</c> without opening a second connection per chat.
    /// </summary>
    public static async Task<Message?> GetLastMessageForChatAsync(
        SqliteConnection connection, ChatDatabaseSchema schema, HandleDirectory handleDirectory, string chatGuid, CancellationToken cancellationToken)
    {
        MessageRow? row = null;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"""
                SELECT
                {MessageRowMapper.BuildSelectColumns(schema)}
                FROM message m
                JOIN chat_message_join cmj ON cmj.message_id = m.ROWID
                JOIN chat c ON c.ROWID = cmj.chat_id
                WHERE c.guid = @chatGuid
                  AND (m.associated_message_type IS NULL OR m.associated_message_type NOT BETWEEN 2000 AND 3006)
                ORDER BY m.date DESC
                LIMIT 1
                """;
            command.Parameters.AddWithValue("@chatGuid", chatGuid);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                row = MessageRowMapper.ReadRow(reader);
            }
        }

        return row is null
            ? null
            : await MessageRowAssembler.AssembleAsync(connection, schema, handleDirectory, row, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<List<MessageRow>> FetchPrimaryRowsAsync(
        SqliteConnection connection, ChatDatabaseSchema schema, MessageQueryCriteria criteria, bool applyPaging, CancellationToken cancellationToken)
    {
        var (whereSql, parameters, needsHandleJoin) = MessageQuerySqlBuilder.Build(criteria, schema);
        var orderSql = criteria.SortOrder == MessageSortOrder.SentAtDescending ? "DESC" : "ASC";

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT
            {MessageRowMapper.BuildSelectColumns(schema)}
            FROM message m
            JOIN chat_message_join cmj ON cmj.message_id = m.ROWID
            JOIN chat c ON c.ROWID = cmj.chat_id
            {(needsHandleJoin ? "LEFT JOIN handle h ON h.ROWID = m.handle_id" : string.Empty)}
            WHERE {whereSql}
            ORDER BY m.date {orderSql}
            {(applyPaging ? "LIMIT @take OFFSET @skip" : string.Empty)}
            """;

        foreach (var parameter in parameters)
        {
            command.Parameters.Add(parameter);
        }

        if (applyPaging)
        {
            command.Parameters.AddWithValue("@take", criteria.TakeCount ?? -1);
            command.Parameters.AddWithValue("@skip", criteria.SkipCount ?? 0);
        }

        var rows = new List<MessageRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var row = MessageRowMapper.ReadRow(reader);
            if (!MessageRowMapper.IsReactionRow(row))
            {
                rows.Add(row);
            }
        }

        return rows;
    }

    private static bool MatchesContaining(string? haystack, string needle, StringComparison comparison) =>
        haystack is not null && haystack.Contains(needle, comparison);
}
