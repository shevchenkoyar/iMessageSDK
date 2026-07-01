using Microsoft.Data.Sqlite;

namespace iMessageSDK.Internal.Database;

/// <summary>
/// Introspects which optional columns exist in the current conversation history file, since the
/// on-disk shape drifts across macOS releases. Every optional-column read elsewhere in the
/// implementation goes through this type rather than assuming a fixed schema.
/// </summary>
internal sealed class ChatDatabaseSchema
{
    private readonly IReadOnlySet<string> _messageColumns;
    private readonly IReadOnlySet<string> _chatColumns;
    private readonly IReadOnlySet<string> _attachmentColumns;

    private ChatDatabaseSchema(
        IReadOnlySet<string> messageColumns,
        IReadOnlySet<string> chatColumns,
        IReadOnlySet<string> attachmentColumns)
    {
        _messageColumns = messageColumns;
        _chatColumns = chatColumns;
        _attachmentColumns = attachmentColumns;
    }

    public bool IsRecognizedSchema =>
        _messageColumns.Contains("guid") && _messageColumns.Contains("date")
        && _chatColumns.Contains("guid") && _chatColumns.Contains("chat_identifier")
        && _attachmentColumns.Contains("guid") && _attachmentColumns.Contains("filename");

    public bool MessageHasColumn(string name) => _messageColumns.Contains(name);

    public bool ChatHasColumn(string name) => _chatColumns.Contains(name);

    public bool AttachmentHasColumn(string name) => _attachmentColumns.Contains(name);

    public static async Task<ChatDatabaseSchema> LoadAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var message = await GetColumnNamesAsync(connection, KnownTable.Message, cancellationToken).ConfigureAwait(false);
        var chat = await GetColumnNamesAsync(connection, KnownTable.Chat, cancellationToken).ConfigureAwait(false);
        var attachment = await GetColumnNamesAsync(connection, KnownTable.Attachment, cancellationToken).ConfigureAwait(false);
        return new ChatDatabaseSchema(message, chat, attachment);
    }

    private static async Task<IReadOnlySet<string>> GetColumnNamesAsync(
        SqliteConnection connection, KnownTable table, CancellationToken cancellationToken)
    {
        var columns = new HashSet<string>(StringComparer.Ordinal);

        await using var command = connection.CreateCommand();
        command.CommandText = GetPragmaCommandText(table);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        var nameOrdinal = reader.GetOrdinal("name");
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            columns.Add(reader.GetString(nameOrdinal));
        }

        return columns;
    }

    // PRAGMA does not support parameter binding, so the table name must be a literal. Routing
    // through this fixed enum (rather than interpolating a string) guarantees no external input
    // ever reaches the command text.
    private static string GetPragmaCommandText(KnownTable table) => table switch
    {
        KnownTable.Message => "PRAGMA table_info('message')",
        KnownTable.Chat => "PRAGMA table_info('chat')",
        KnownTable.Attachment => "PRAGMA table_info('attachment')",
        _ => throw new ArgumentOutOfRangeException(nameof(table), table, null),
    };

    private enum KnownTable
    {
        Message,
        Chat,
        Attachment,
    }
}
