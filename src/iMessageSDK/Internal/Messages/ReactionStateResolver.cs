using iMessageSDK.Chats;
using iMessageSDK.Messages;

namespace iMessageSDK.Internal.Messages;

/// <summary>
/// Collapses the raw stream of tapback add/remove events chat.db records into the set of
/// currently-active reactions, since a message's <see cref="Message.Reactions"/> should reflect
/// its current state rather than every historical event.
/// </summary>
internal static class ReactionStateResolver
{
    public static IReadOnlyList<Reaction> ResolveCurrentState(IEnumerable<Reaction> events)
    {
        var latestBySignature = new Dictionary<(ParticipantId Sender, TapbackKind Kind, string? Emoji), Reaction>();

        foreach (var reaction in events)
        {
            var key = (reaction.Sender.Id, reaction.Kind, reaction.EmojiValue);
            if (!latestBySignature.TryGetValue(key, out var existing) || reaction.ReactedAt >= existing.ReactedAt)
            {
                latestBySignature[key] = reaction;
            }
        }

        return latestBySignature.Values
            .Where(r => !r.IsRemoved)
            .OrderBy(r => r.ReactedAt)
            .ToList();
    }
}
