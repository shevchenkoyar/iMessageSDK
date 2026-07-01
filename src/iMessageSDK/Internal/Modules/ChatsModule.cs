using iMessageSDK.Chats;
using iMessageSDK.Internal.Querying;

namespace iMessageSDK.Internal.Modules;

/// <summary>The concrete <see cref="IChatsModule"/> implementation.</summary>
internal sealed class ChatsModule : ChatQuery, IChatsModule
{
    private readonly ChatQueryExecutor _executor;

    public ChatsModule(string databasePath)
        : this(new ChatQueryExecutor(databasePath))
    {
    }

    private ChatsModule(ChatQueryExecutor executor)
        : base(executor, new ChatQueryCriteria())
    {
        _executor = executor;
    }

    public Task<Chat?> GetAsync(ChatId id, CancellationToken cancellationToken = default) =>
        _executor.GetAsync(id, cancellationToken);
}
