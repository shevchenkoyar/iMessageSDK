using Microsoft.Data.Sqlite;

namespace iMessageSDK.Internal.Database;

/// <summary>
/// Resolves the location of the Messages conversation history file and opens read-only
/// connections to it.
/// </summary>
internal static class ChatDatabaseConnectionFactory
{
    /// <summary>The default location of the Messages conversation history file.</summary>
    public static string DefaultDatabasePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "Messages", "chat.db");

    /// <summary>
    /// Opens a new read-only connection to the conversation history file at
    /// <paramref name="databasePath"/>.
    /// </summary>
    /// <remarks>
    /// The connection is opened in read-only mode: Messages.app keeps its own read-write handle
    /// open at all times, and SQLite supports multiple concurrent readers against a single
    /// writer, so this does not interfere with the running application.
    /// </remarks>
    public static async Task<SqliteConnection> OpenAsync(string databasePath, CancellationToken cancellationToken)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadOnly,
        };

        var connection = new SqliteConnection(connectionStringBuilder.ToString());
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }
}
