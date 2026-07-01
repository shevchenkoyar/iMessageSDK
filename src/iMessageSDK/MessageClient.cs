using System.Runtime.Versioning;
using iMessageSDK.Attachments;
using iMessageSDK.Chats;
using iMessageSDK.Diagnostics;
using iMessageSDK.Exceptions;
using iMessageSDK.Internal.Database;
using iMessageSDK.Internal.Diagnostics;
using iMessageSDK.Internal.Modules;
using iMessageSDK.Messages;

namespace iMessageSDK;

/// <summary>
/// The default <see cref="IMessageClient"/> implementation, backed by the macOS Messages
/// application's conversation history and automation interfaces.
/// </summary>
/// <remarks>
/// Construct instances via <see cref="CreateAsync"/> rather than directly; the SDK's internal
/// implementation types are intentionally not part of the public API.
/// </remarks>
[SupportedOSPlatform("macos")]
public sealed class MessageClient : IMessageClient
{
    private readonly MessagesModule _messages;

    private MessageClient(MessagesModule messages, IChatsModule chats, IAttachmentsModule attachments, IDiagnosticsModule diagnostics)
    {
        _messages = messages;
        Messages = messages;
        Chats = chats;
        Attachments = attachments;
        Diagnostics = diagnostics;
    }

    /// <inheritdoc />
    public IMessagesModule Messages { get; }

    /// <inheritdoc />
    public IChatsModule Chats { get; }

    /// <inheritdoc />
    public IAttachmentsModule Attachments { get; }

    /// <inheritdoc />
    public IDiagnosticsModule Diagnostics { get; }

    /// <summary>
    /// Creates a new <see cref="IMessageClient"/>.
    /// </summary>
    /// <param name="options">Optional configuration; when <see langword="null"/>, defaults are used.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A ready-to-use client. No connection is opened, and no permissions are checked, until a query, send, or watch operation is performed.</returns>
    /// <exception cref="UnsupportedPlatformException">The current operating system is not macOS.</exception>
    public static Task<IMessageClient> CreateAsync(MessageClientOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (!OperatingSystem.IsMacOS())
        {
            throw new UnsupportedPlatformException();
        }

        var resolvedOptions = options ?? new MessageClientOptions();
        var databasePath = resolvedOptions.MessagesDatabasePath ?? ChatDatabaseConnectionFactory.DefaultDatabasePath;

        var messages = new MessagesModule(databasePath, resolvedOptions.PollingInterval);
        var chats = new ChatsModule(databasePath);
        var attachments = new AttachmentsModule(databasePath);
        var diagnostics = new DiagnosticsModule(databasePath);

        IMessageClient client = new MessageClient(messages, chats, attachments, diagnostics);
        return Task.FromResult(client);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _messages.DisposeAsync();
}
