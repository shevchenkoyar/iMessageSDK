namespace iMessageSDK.Internal.Sending;

/// <summary>
/// The AppleScript sources used to automate the Messages application. Every dynamic value (chat
/// id, message text, file path) is read from <c>argv</c> rather than interpolated into the
/// script text, so no escaping is needed and no injection is possible: process arguments are
/// passed to <c>osascript</c> as separate, already-delimited values.
/// </summary>
internal static class AppleScriptTemplates
{
    public const string SendText = """
        on run argv
            set targetChatId to item 1 of argv
            set messageText to item 2 of argv
            tell application "Messages"
                set targetChat to a reference to chat id targetChatId
                send messageText to targetChat
            end tell
        end run
        """;

    public const string SendAttachment = """
        on run argv
            set targetChatId to item 1 of argv
            set filePath to item 2 of argv
            tell application "Messages"
                set targetChat to a reference to chat id targetChatId
                send (POSIX file filePath) to targetChat
            end tell
        end run
        """;

    public const string GetMessagesApplicationName = """
        tell application "Messages" to get name
        """;
}
