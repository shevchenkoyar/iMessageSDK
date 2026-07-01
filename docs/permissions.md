# Permissions Setup

iMessageSDK needs two macOS permissions granted to whatever process hosts it (your app, a
console host, `dotnet run`'s host process, etc.). macOS requires a human to grant these through
System Settings — no API lets an app grant them to itself. Use
[`client.Diagnostics`](diagnostics.md) to check status programmatically.

## 1. Full Disk Access

Required to read `~/Library/Messages/chat.db` (reading messages, chats, attachments metadata).

1. Open **System Settings → Privacy & Security → Full Disk Access**.
2. Click **+** and add the application/binary that hosts your process (for a `dotnet run` app
   during development, this is typically the `dotnet` executable itself, or your IDE).
3. Ensure the toggle next to it is enabled.
4. Restart the host process — macOS only re-evaluates this permission at process launch.

Without this, reads throw `FullDiskAccessDeniedException`.

## 2. Automation access to control "Messages"

Required to send messages (iMessageSDK automates Messages via AppleScript).

1. Open **System Settings → Privacy & Security → Automation**.
2. Find your host application in the list.
3. Enable the checkbox for **Messages** underneath it.

If your app isn't listed yet, macOS usually prompts for this automatically the first time a send
is attempted — approve the prompt. If you previously denied it, you'll need to enable it manually
here, since macOS won't prompt again.

Without this, sending throws `AutomationPermissionDeniedException`.

## Verifying

```csharp
var report = await client.Diagnostics.RunAsync();
Console.WriteLine($"Full Disk Access: {report.FullDiskAccess.IsGranted}");
Console.WriteLine($"Automation: {report.AutomationPermission.IsGranted}");
Console.WriteLine($"Messages installed: {report.MessagesAppAvailability.IsInstalled}");
Console.WriteLine($"Messages running: {report.MessagesAppAvailability.IsRunning}");
```
