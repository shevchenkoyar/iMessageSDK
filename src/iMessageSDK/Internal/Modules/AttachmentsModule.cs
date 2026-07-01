using iMessageSDK.Attachments;
using iMessageSDK.Exceptions;
using iMessageSDK.Internal.Attachments;
using iMessageSDK.Internal.Database;

namespace iMessageSDK.Internal.Modules;

/// <summary>The concrete <see cref="IAttachmentsModule"/> implementation.</summary>
internal sealed class AttachmentsModule : IAttachmentsModule
{
    private readonly string _databasePath;

    public AttachmentsModule(string databasePath)
    {
        _databasePath = databasePath;
    }

    public async Task<Attachment?> GetAsync(AttachmentId id, CancellationToken cancellationToken = default)
    {
        await using var connection = await ChatDatabaseConnectionFactory.OpenAsync(_databasePath, cancellationToken).ConfigureAwait(false);
        var schema = await ChatDatabaseSchema.LoadAsync(connection, cancellationToken).ConfigureAwait(false);

        AttachmentRow? row;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = $"""
                SELECT
                {AttachmentRowMapper.BuildSelectColumns(schema)}
                FROM attachment a
                WHERE a.guid = @guid
                LIMIT 1
                """;
            command.Parameters.AddWithValue("@guid", id.Value);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            row = await reader.ReadAsync(cancellationToken).ConfigureAwait(false) ? AttachmentRowMapper.ReadRow(reader) : null;
        }

        return row is null ? null : await AttachmentMapper.MapAsync(row, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Stream> OpenReadAsync(AttachmentId id, CancellationToken cancellationToken = default)
    {
        var attachment = await GetAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new AttachmentNotAvailableException($"No attachment with id '{id}' was found.");
        return await OpenReadAsync(attachment, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Stream> OpenReadAsync(Attachment attachment, CancellationToken cancellationToken = default)
    {
        if (attachment.TransferState != AttachmentTransferState.Downloaded)
        {
            throw new AttachmentNotAvailableException(
                $"The attachment '{attachment.Id}' is not currently available (transfer state: {attachment.TransferState}).");
        }

        var path = await ResolvePathAsync(attachment.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new AttachmentNotAvailableException($"The attachment '{attachment.Id}' has no associated file path.");

        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
    }

    private async Task<string?> ResolvePathAsync(AttachmentId id, CancellationToken cancellationToken)
    {
        await using var connection = await ChatDatabaseConnectionFactory.OpenAsync(_databasePath, cancellationToken).ConfigureAwait(false);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT filename FROM attachment WHERE guid = @guid LIMIT 1";
        command.Parameters.AddWithValue("@guid", id.Value);
        var raw = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
        return AttachmentPathResolver.ResolveAbsolutePath(raw);
    }
}
