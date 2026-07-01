using iMessageSDK.Attachments;
using iMessageSDK.Chats;
using iMessageSDK.Messages;
using iMessageSDK.Tests.Fixtures;

namespace iMessageSDK.Tests;

public class MessagesQueryTests : IAsyncLifetime
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
    public async Task WhereChat_ReturnsOnlyMessagesFromThatChat()
    {
        var messages = await _client.Messages.WhereChat(new ChatId(_database.DirectChatGuid)).ToListAsync();

        Assert.NotEmpty(messages);
        Assert.All(messages, m => Assert.Equal(_database.DirectChatGuid, m.ChatId.Value));
        Assert.Contains(messages, m => m.Text == "Hello there");
    }

    [Fact]
    public async Task Containing_FindsMatchingMessages()
    {
        var messages = await _client.Messages.Containing("beach").ToListAsync();

        Assert.Single(messages);
        Assert.Equal("Let's go to the beach", messages[0].Text);
    }

    [Fact]
    public async Task DeletedMessages_AreExcludedByDefault_AndIncludedWithIncludeDeleted()
    {
        var withoutDeleted = await _client.Messages.WhereChat(new ChatId(_database.DirectChatGuid)).ToListAsync();
        Assert.DoesNotContain(withoutDeleted, m => m.Text == "oops deleting this");

        var withDeleted = await _client.Messages.WhereChat(new ChatId(_database.DirectChatGuid)).IncludeDeleted().ToListAsync();
        var deleted = Assert.Single(withDeleted, m => m.Text == "oops deleting this");
        Assert.NotNull(deleted.DeletionInfo);
    }

    [Fact]
    public async Task IncomingMessage_WithoutReadReceipt_IsDelivered()
    {
        var message = await _client.Messages.WhereChat(new ChatId(_database.DirectChatGuid)).ToListAsync();
        var hello = Assert.Single(message, m => m.Text == "Hello there");

        Assert.False(hello.IsFromMe);
        Assert.Equal(MessageStatus.Delivered, hello.Status);
        Assert.Equal(_database.AliceHandle, hello.Sender?.Id.Value);
    }

    [Fact]
    public async Task OutgoingMessage_WithReadReceipt_IsRead()
    {
        var messages = await _client.Messages.WhereChat(new ChatId(_database.DirectChatGuid)).ToListAsync();
        var hi = Assert.Single(messages, m => m.Text == "Hi Alice!");

        Assert.True(hi.IsFromMe);
        Assert.Equal(MessageStatus.Read, hi.Status);
    }

    [Fact]
    public async Task EditedMessage_HasEditInfo()
    {
        var messages = await _client.Messages.WhereChat(new ChatId(_database.DirectChatGuid)).ToListAsync();
        var edited = Assert.Single(messages, m => m.Text == "This got edited");

        Assert.NotNull(edited.EditInfo);
        Assert.NotEmpty(edited.EditInfo.History);
    }

    [Fact]
    public async Task ReplyMessage_HasReplyMetadata()
    {
        var messages = await _client.Messages.WhereChat(new ChatId(_database.DirectChatGuid)).ToListAsync();
        var reply = Assert.Single(messages, m => m.Text == "Replying to hello");

        Assert.NotNull(reply.ReplyTo);
        Assert.Equal(_database.HelloMessageGuid, reply.ReplyTo.RepliedToMessageId.Value);
        Assert.Equal("Hello there", reply.ReplyTo.RepliedToPreviewText);
    }

    [Fact]
    public async Task AttachmentOnlyMessage_HasDownloadedImageAttachment()
    {
        var messages = await _client.Messages.WhereChat(new ChatId(_database.DirectChatGuid)).WithAttachments().ToListAsync();
        var photo = Assert.Single(messages);

        Assert.Equal(MessageKind.Attachment, photo.Kind);
        var attachment = Assert.Single(photo.Attachments);
        Assert.Equal(AttachmentKind.Image, attachment.Kind);
        Assert.Equal(AttachmentTransferState.Downloaded, attachment.TransferState);
        Assert.Equal("image/jpeg", attachment.MimeType);
    }

    [Fact]
    public async Task GroupChatMessage_ReceivesActiveReaction()
    {
        var messages = await _client.Messages.WhereChat(new ChatId(_database.GroupChatGuid)).ToListAsync();
        var target = Assert.Single(messages, m => m.Text == "Sounds great");

        var reaction = Assert.Single(target.Reactions);
        Assert.Equal(TapbackKind.Loved, reaction.Kind);
        Assert.Equal(_database.CarolHandle, reaction.Sender.Id.Value);
        Assert.False(reaction.IsRemoved);
    }

    [Fact]
    public async Task GroupActionMessage_HasDescription()
    {
        var messages = await _client.Messages.WhereChat(new ChatId(_database.GroupChatGuid)).OfKind(MessageKind.GroupAction).ToListAsync();

        var action = Assert.Single(messages);
        Assert.False(string.IsNullOrWhiteSpace(action.GroupActionDescription));
    }

    [Fact]
    public async Task Take_LimitsResultCount()
    {
        var messages = await _client.Messages.WhereChat(new ChatId(_database.DirectChatGuid)).Take(2).ToListAsync();

        Assert.Equal(2, messages.Count);
    }

    [Fact]
    public async Task GetAsync_ReturnsMessageByStableId()
    {
        var message = await _client.Messages.GetAsync(new MessageId(_database.HelloMessageGuid));

        Assert.NotNull(message);
        Assert.Equal("Hello there", message.Text);
    }
}
