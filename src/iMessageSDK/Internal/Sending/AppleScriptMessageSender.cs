using iMessageSDK.Chats;
using iMessageSDK.Exceptions;
using iMessageSDK.Internal.Chats;
using iMessageSDK.Internal.Database;
using iMessageSDK.Internal.Querying;
using iMessageSDK.Messages;

namespace iMessageSDK.Internal.Sending;

/// <summary>
/// Sends messages by automating the Messages application via AppleScript, then confirms the send
/// by polling the conversation history for the resulting row.
/// </summary>
/// <remarks>
/// AppleScript's <c>send</c> command does not return the resulting message's identity, so after
/// invoking it, the conversation history is polled briefly for the newest outgoing message in the
/// target chat, which is then mapped and returned. This gives callers a real
/// <see cref="Message"/> (with its actual identifier and timestamp) rather than a
/// synthesized placeholder.
/// </remarks>
internal sealed class AppleScriptMessageSender
{
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    private readonly string _databasePath;

    public AppleScriptMessageSender(string databasePath)
    {
        _databasePath = databasePath;
    }

    public async Task<Message> SendAsync(ChatId chatId, MessageContent content, CancellationToken cancellationToken)
    {
        if (content.Text is not { Length: > 0 } && content.Attachments.Count == 0)
        {
            throw new ArgumentException("Message content must include text, at least one attachment, or both.", nameof(content));
        }

        var sentAfter = DateTimeOffset.UtcNow;

        if (content.Text is { Length: > 0 } text)
        {
            await RunAndThrowOnFailureAsync(AppleScriptTemplates.SendText, [chatId.Value, text], cancellationToken).ConfigureAwait(false);
        }

        foreach (var attachment in content.Attachments)
        {
            if (!File.Exists(attachment.FilePath))
            {
                throw new FileNotFoundException($"The attachment file '{attachment.FilePath}' does not exist.", attachment.FilePath);
            }

            await RunAndThrowOnFailureAsync(AppleScriptTemplates.SendAttachment, [chatId.Value, attachment.FilePath], cancellationToken).ConfigureAwait(false);
        }

        return await WaitForSentMessageAsync(chatId, sentAfter, cancellationToken).ConfigureAwait(false);
    }

    private static async Task RunAndThrowOnFailureAsync(string script, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var result = await AppleScriptRunner.RunAsync(script, arguments, cancellationToken).ConfigureAwait(false);
        if (result.Succeeded)
        {
            return;
        }

        if (result.IsAutomationPermissionDenied)
        {
            throw new AutomationPermissionDeniedException(
                $"Automation access to control \"Messages\" was denied. Grant it under System Settings > Privacy & Security > Automation. Details: {result.StandardError.Trim()}");
        }

        throw new MessageSendException($"Messages could not send the message (osascript exited with code {result.ExitCode}): {result.StandardError.Trim()}");
    }

    private async Task<Message> WaitForSentMessageAsync(ChatId chatId, DateTimeOffset sentAfter, CancellationToken cancellationToken)
    {
        var sentAfterNanoseconds = AppleTimeConverter.ToAppleNanoseconds(sentAfter);
        var deadline = DateTime.UtcNow.Add(PollTimeout);

        while (true)
        {
            await using var connection = await ChatDatabaseConnectionFactory.OpenAsync(_databasePath, cancellationToken).ConfigureAwait(false);
            var schema = await ChatDatabaseSchema.LoadAsync(connection, cancellationToken).ConfigureAwait(false);
            var handleDirectory = await HandleDirectory.LoadAsync(connection, cancellationToken).ConfigureAwait(false);

            var message = await TryFindSentMessageAsync(connection, schema, handleDirectory, chatId.Value, sentAfterNanoseconds, cancellationToken)
                .ConfigureAwait(false);
            if (message is not null)
            {
                return message;
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new MessageSendException(
                    "Messages accepted the send request, but the resulting message could not be confirmed in the conversation history within the timeout.");
            }

            await Task.Delay(PollInterval, cancellationToken).ConfigureAwait(false);
        }
    }

    private static async Task<Message?> TryFindSentMessageAsync(
        Microsoft.Data.Sqlite.SqliteConnection connection,
        ChatDatabaseSchema schema,
        HandleDirectory handleDirectory,
        string chatGuid,
        long sentAfterNanoseconds,
        CancellationToken cancellationToken)
    {
        MessageRow? row;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"""
                SELECT
                {MessageRowMapper.BuildSelectColumns(schema)}
                FROM message m
                JOIN chat_message_join cmj ON cmj.message_id = m.ROWID
                JOIN chat c ON c.ROWID = cmj.chat_id
                WHERE c.guid = @chatGuid
                  AND m.is_from_me = 1
                  AND m.date >= @sentAfter
                  AND (m.associated_message_type IS NULL OR m.associated_message_type NOT BETWEEN 2000 AND 3006)
                ORDER BY m.date DESC
                LIMIT 1
                """;
            command.Parameters.AddWithValue("@chatGuid", chatGuid);
            command.Parameters.AddWithValue("@sentAfter", sentAfterNanoseconds);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            row = await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? MessageRowMapper.ReadRow(reader) : null;
        }

        return row is null
            ? null
            : await MessageRowAssembler.AssembleAsync(connection, schema, handleDirectory, row, cancellationToken).ConfigureAwait(false);
    }
}
