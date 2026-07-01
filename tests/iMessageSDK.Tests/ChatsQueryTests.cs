using iMessageSDK.Chats;
using iMessageSDK.Messages;
using iMessageSDK.Tests.Fixtures;

namespace iMessageSDK.Tests;

public class ChatsQueryTests : IAsyncLifetime
{
    private TestChatDatabase _database = null!;
    private IMessageClient _client = null!;

    public async Task InitializeAsync()
    {
        _database = await TestChatDatabase.CreateAsync();
        _client = await MessageClient.CreateAsync(new MessageClientOptions { MessagesDatabasePath = _database.Path });
    }

    public async Task DisposeAsync()
    {
        await _client.DisposeAsync();
        await _database.DisposeAsync();
    }

    [Fact]
    public async Task ToListAsync_ReturnsBothChats()
    {
        var chats = await _client.Chats.ToListAsync();

        Assert.Equal(2, chats.Count);
    }

    [Fact]
    public async Task DirectChat_IsClassifiedAsDirectWithFallbackDisplayName()
    {
        var chat = await _client.Chats.GetAsync(new ChatId(_database.DirectChatGuid));

        Assert.NotNull(chat);
        Assert.Equal(ChatKind.Direct, chat.Kind);
        Assert.Equal(_database.AliceHandle, chat.DisplayName);
        Assert.Contains(chat.Participants, p => p.IsMe);
        Assert.Contains(chat.Participants, p => p.Id.Value == _database.AliceHandle);
    }

    [Fact]
    public async Task GroupChat_IsClassifiedAsGroupWithCustomDisplayName()
    {
        var chat = await _client.Chats.GetAsync(new ChatId(_database.GroupChatGuid));

        Assert.NotNull(chat);
        Assert.Equal(ChatKind.Group, chat.Kind);
        Assert.Equal("Trip Planning", chat.DisplayName);
        Assert.Equal(3, chat.Participants.Count); // me + Bob + Carol
    }

    [Fact]
    public async Task WhereKind_FiltersToGroupChatsOnly()
    {
        var groupChats = await _client.Chats.WhereKind(ChatKind.Group).ToListAsync();

        var chat = Assert.Single(groupChats);
        Assert.Equal(_database.GroupChatGuid, chat.Id.Value);
    }

    [Fact]
    public async Task LastMessage_IsTheMostRecentMessageInTheChat()
    {
        var chat = await _client.Chats.GetAsync(new ChatId(_database.DirectChatGuid));

        Assert.NotNull(chat!.LastMessage);
        Assert.Equal("Replying to hello", chat.LastMessage.Text);
    }

    [Fact]
    public async Task GroupChatLastMessage_IsTheGroupActionEvent()
    {
        var chat = await _client.Chats.GetAsync(new ChatId(_database.GroupChatGuid));
        
        Assert.NotNull(chat!.LastMessage);
        Assert.Equal(MessageKind.GroupAction, chat.LastMessage.Kind);
    }
}
