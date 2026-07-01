using iMessageSDK.Attachments;
using iMessageSDK.Chats;
using iMessageSDK.Diagnostics;
using iMessageSDK.Messages;

namespace iMessageSDK;

/// <summary>
/// The root entry point for interacting with the macOS Messages application.
/// </summary>
/// <remarks>
/// Obtain an instance via <c>MessageClient.CreateAsync</c> (in the <c>iMessageSDK</c> package),
/// or by registering the SDK with dependency injection via <c>AddIMessageSdk</c> (in the
/// <c>iMessageSDK.Extensions.DependencyInjection</c> package). Dispose the client — or let the
/// dependency injection container dispose it — when you are done; disposal stops any active
/// watchers and releases underlying resources.
/// </remarks>
public interface IMessageClient : IAsyncDisposable
{
    /// <summary>Reading, searching, sending, and watching messages.</summary>
    IMessagesModule Messages { get; }

    /// <summary>Reading and querying chats.</summary>
    IChatsModule Chats { get; }

    /// <summary>Retrieving attachment metadata and content.</summary>
    IAttachmentsModule Attachments { get; }

    /// <summary>Diagnosing macOS permissions and Messages availability.</summary>
    IDiagnosticsModule Diagnostics { get; }
}
