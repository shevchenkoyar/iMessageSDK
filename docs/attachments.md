# Attachments

`Attachment` is a closed hierarchy — `MediaAttachment`, `ContactAttachment`, `LocationAttachment`,
and `LivePhotoAttachment` are the only derived types — so you can pattern-match exhaustively:

```csharp
foreach (var attachment in message.Attachments)
{
    var description = attachment switch
    {
        MediaAttachment { Kind: AttachmentKind.VoiceMessage } m => $"Voice message ({m.Duration})",
        MediaAttachment m => $"{m.Kind} ({m.Width}x{m.Height})",
        ContactAttachment c => $"Contact: {c.Contact.DisplayName}",
        LocationAttachment l => $"Location: {l.Coordinate.Latitude}, {l.Coordinate.Longitude}",
        LivePhotoAttachment lp => "Live Photo",
        _ => "Unknown",
    };
}
```

## Opening content

Metadata (`FileName`, `MimeType`, `SizeInBytes`, `TransferState`) is available directly on the
attachment. To read the actual bytes:

```csharp
if (attachment.TransferState == AttachmentTransferState.Downloaded)
{
    await using var stream = await client.Attachments.OpenReadAsync(attachment);
    // ...
}
```

`OpenReadAsync` throws `AttachmentNotAvailableException` if the content isn't currently available
— for example, an MMS attachment that hasn't finished downloading from iCloud. Check
`TransferState` first, or subscribe to `IMessageWatcher.AttachmentDownloaded` to be notified when a
pending attachment becomes available.

## Live Photos

A Live Photo's still image and its short companion video are stored as two separate attachments
sharing a base file name. iMessageSDK recognizes this pairing and folds it into a single
`LivePhotoAttachment`: `Id` refers to the still image, `MotionVideoAttachmentId` refers to the
companion video, which remains independently retrievable via `client.Attachments.GetAsync(...)`.

## Known limitation: locations

Apple typically delivers a shared location as an interactive message-extension payload, not a
plain file attachment — recognizing that format is out of scope for this version.
`LocationAttachment` exists in the domain model for forward compatibility, but is not currently
produced by the mapping layer. `ContactAttachment` (vCard) *is* fully supported, since vCard is a
simple, publicly documented text format.
