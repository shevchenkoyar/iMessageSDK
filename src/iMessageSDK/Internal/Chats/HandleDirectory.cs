using iMessageSDK.Chats;
using Microsoft.Data.Sqlite;

namespace iMessageSDK.Internal.Chats;

/// <summary>
/// A snapshot of the <c>handle</c> table (the participants Messages knows about), letting rows
/// that reference a handle by its internal row id be resolved to a <see cref="Participant"/>
/// without a per-row join.
/// </summary>
/// <remarks>
/// The handle table is small (one row per known participant) and changes rarely, so callers load
/// a fresh snapshot at the start of an operation rather than joining it into every query.
/// </remarks>
internal sealed class HandleDirectory
{
    /// <summary>The participant representing the local account, used for outgoing messages.</summary>
    public static Participant Me { get; } = new() { Id = new ParticipantId("me"), IsMe = true };

    private readonly IReadOnlyDictionary<long, Participant> _participantsByHandleId;

    private HandleDirectory(IReadOnlyDictionary<long, Participant> participantsByHandleId)
    {
        _participantsByHandleId = participantsByHandleId;
    }

    public static async Task<HandleDirectory> LoadAsync(SqliteConnection connection, CancellationToken cancellationToken)
    {
        var map = new Dictionary<long, Participant>();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT ROWID, id FROM handle";

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var rowId = reader.GetInt64(0);
            var handle = reader.GetString(1);
            map[rowId] = new Participant { Id = new ParticipantId(handle), IsMe = false };
        }

        return new HandleDirectory(map);
    }

    /// <summary>Resolves the sender of a message row, accounting for the local account sending it.</summary>
    public Participant ResolveSender(long? handleId, bool isFromMe) => isFromMe ? Me : ResolveHandle(handleId);

    /// <summary>Resolves a raw handle row id to a participant, falling back to a placeholder for an unrecognized id.</summary>
    public Participant ResolveHandle(long? handleId)
    {
        if (handleId is { } id && _participantsByHandleId.TryGetValue(id, out var participant))
        {
            return participant;
        }

        return new Participant { Id = new ParticipantId(handleId?.ToString() ?? "unknown"), IsMe = false };
    }
}
