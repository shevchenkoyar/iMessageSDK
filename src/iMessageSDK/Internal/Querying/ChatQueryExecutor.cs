using System.Runtime.CompilerServices;
using iMessageSDK.Chats;
using iMessageSDK.Internal.Chats;
using iMessageSDK.Internal.Database;
using Microsoft.Data.Sqlite;

namespace iMessageSDK.Internal.Querying;

/// <summary>
/// Executes <see cref="ChatQueryCriteria"/> against the conversation history file, mapping raw
/// rows to fully-assembled domain <see cref="Chat"/> instances (including participants and each
/// chat's last message).
/// </summary>
internal sealed class ChatQueryExecutor
{
    private readonly string _databasePath;

    public ChatQueryExecutor(string databasePath)
    {
        _databasePath = databasePath;
    }

    public async Task<IReadOnlyList<Chat>> ToListAsync(ChatQueryCriteria criteria, CancellationToken cancellationToken)
    {
        var results = new List<Chat>();
        await foreach (var chat in AsAsyncEnumerable(criteria, cancellationToken).ConfigureAwait(false))
        {
            results.Add(chat);
        }

        return results;
    }

    public async Task<Chat?> FirstOrDefaultAsync(ChatQueryCriteria criteria, CancellationToken cancellationToken)
    {
        var narrowed = criteria with { TakeCount = 1 };
        await foreach (var chat in AsAsyncEnumerable(narrowed, cancellationToken).ConfigureAwait(false))
        {
            return chat;
        }

        return null;
    }

    public async Task<int> CountAsync(ChatQueryCriteria criteria, CancellationToken cancellationToken)
    {
        var unpaged = criteria with { SkipCount = null, TakeCount = null };
        var count = 0;
        await foreach (var _ in AsAsyncEnumerable(unpaged, cancellationToken).ConfigureAwait(false))
        {
            count++;
        }

        return count;
    }

    public async Task<Chat?> GetAsync(ChatId id, CancellationToken cancellationToken)
    {
        await using var connection = await ChatDatabaseConnectionFactory.OpenAsync(_databasePath, cancellationToken).ConfigureAwait(false);
        var schema = await ChatDatabaseSchema.LoadAsync(connection, cancellationToken).ConfigureAwait(false);
        var handleDirectory = await HandleDirectory.LoadAsync(connection, cancellationToken).ConfigureAwait(false);

        ChatRow? row;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"""
                SELECT
                {ChatRowMapper.BuildSelectColumns(schema)}
                FROM chat c
                WHERE c.guid = @chatGuid
                LIMIT 1
                """;
            command.Parameters.AddWithValue("@chatGuid", id.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            row = await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? ChatRowMapper.ReadRow(reader) : null;
        }

        if (row is null)
        {
            return null;
        }

        var participants = await LoadParticipantsAsync(connection, row.RowId, handleDirectory, cancellationToken).ConfigureAwait(false);
        var lastMessage = await MessageQueryExecutor.GetLastMessageForChatAsync(connection, schema, handleDirectory, row.Guid, cancellationToken).ConfigureAwait(false);
        return ChatMapper.Map(row, participants, lastMessage);
    }

    public async IAsyncEnumerable<Chat> AsAsyncEnumerable(
        ChatQueryCriteria criteria, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await using var connection = await ChatDatabaseConnectionFactory.OpenAsync(_databasePath, cancellationToken).ConfigureAwait(false);
        var schema = await ChatDatabaseSchema.LoadAsync(connection, cancellationToken).ConfigureAwait(false);
        var handleDirectory = await HandleDirectory.LoadAsync(connection, cancellationToken).ConfigureAwait(false);

        var clauses = new List<SqliteParameter>();
        var whereFragments = new List<string>();
        if (criteria.DisplayNameContains is { } displayNameContains)
        {
            whereFragments.Add("c.display_name LIKE '%' || @displayNameContains || '%'");
            clauses.Add(new SqliteParameter("@displayNameContains", displayNameContains));
        }

        var whereSql = whereFragments.Count > 0 ? "WHERE " + string.Join(" AND ", whereFragments) : string.Empty;
        var orderSql = criteria.LastActivityDirection == SortDirection.Ascending ? "ASC" : "DESC";

        var rows = new List<ChatRow>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"""
                SELECT
                {ChatRowMapper.BuildSelectColumns(schema)},
                (SELECT MAX(m2.date) FROM message m2
                 JOIN chat_message_join cmj2 ON cmj2.message_id = m2.ROWID
                 WHERE cmj2.chat_id = c.ROWID) AS LastActivity
                FROM chat c
                {whereSql}
                ORDER BY LastActivity {orderSql}
                """;
            foreach (var parameter in clauses)
            {
                command.Parameters.Add(parameter);
            }

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add(ChatRowMapper.ReadRow(reader));
            }
        }

        var skipRemaining = criteria.SkipCount ?? 0;
        var takeRemaining = criteria.TakeCount ?? int.MaxValue;
        var emitted = 0;

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var participants = await LoadParticipantsAsync(connection, row.RowId, handleDirectory, cancellationToken).ConfigureAwait(false);

            if (criteria.Kind is { } kind)
            {
                var otherParticipantCount = participants.Count(p => !p.IsMe);
                var actualKind = otherParticipantCount >= 2 ? ChatKind.Group : ChatKind.Direct;
                if (actualKind != kind)
                {
                    continue;
                }
            }

            if (criteria.ParticipantId is { } participantId && !participants.Any(p => p.Id.Equals(participantId)))
            {
                continue;
            }

            if (skipRemaining > 0)
            {
                skipRemaining--;
                continue;
            }

            if (emitted >= takeRemaining)
            {
                yield break;
            }

            var lastMessage = await MessageQueryExecutor.GetLastMessageForChatAsync(connection, schema, handleDirectory, row.Guid, cancellationToken).ConfigureAwait(false);
            emitted++;
            yield return ChatMapper.Map(row, participants, lastMessage);
        }
    }

    private static async Task<IReadOnlyList<Participant>> LoadParticipantsAsync(
        SqliteConnection connection, long chatRowId, HandleDirectory handleDirectory, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT handle_id FROM chat_handle_join WHERE chat_id = @chatId";
        command.Parameters.AddWithValue("@chatId", chatRowId);

        var participants = new List<Participant> { HandleDirectory.Me };
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            participants.Add(handleDirectory.ResolveHandle(reader.GetInt64(0)));
        }

        return participants;
    }
}
