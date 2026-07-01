namespace iMessageSDK.Chats;

/// <summary>
/// The entry point for reading and querying chats.
/// </summary>
/// <remarks>
/// <see cref="IChatsModule"/> extends <see cref="IChatQuery"/> so queries can be started directly
/// from <c>client.Chats</c>, for example:
/// <code>
/// var groupChats = await client.Chats
///     .WhereKind(ChatKind.Group)
///     .OrderByLastActivity()
///     .Take(20)
///     .ToListAsync();
/// </code>
/// This interface is intended to be consumed, not implemented, by SDK users; new members may be
/// added in future minor versions without that being considered a breaking change.
/// </remarks>
public interface IChatsModule : IChatQuery
{
    /// <summary>Retrieves a single chat by its identifier, or <see langword="null"/> if it does not exist.</summary>
    Task<Chat?> GetAsync(ChatId id, CancellationToken cancellationToken = default);
}
