using iMessageSDK.Attachments;
using iMessageSDK.Internal.Database;

namespace iMessageSDK.Internal.Attachments;

/// <summary>
/// Maps a raw <see cref="AttachmentRow"/> to the appropriate closed-hierarchy
/// <see cref="Attachment"/> subtype.
/// </summary>
/// <remarks>
/// Kind detection and content availability are both determined heuristically (from MIME
/// type/UTI and from whether the referenced file currently exists on disk, respectively), since
/// chat.db does not expose a single authoritative "kind" column. Location-share attachments are
/// not recognized in this version: Apple delivers them as an interactive message-extension
/// payload rather than a plain file attachment, which is out of scope for file-based mapping.
/// </remarks>
internal static class AttachmentMapper
{
    public static async Task<Attachment> MapAsync(AttachmentRow row, CancellationToken cancellationToken)
    {
        var absolutePath = AttachmentPathResolver.ResolveAbsolutePath(row.FileName);
        var transferState = DetermineTransferState(row, absolutePath);
        var kind = DetermineKind(row);
        var createdAt = AppleTimeConverter.ToDateTimeOffset(row.CreatedDate) ?? DateTimeOffset.UnixEpoch;
        var id = new AttachmentId(row.Guid);
        var fileName = row.TransferName ?? (absolutePath is not null ? Path.GetFileName(absolutePath) : null);

        if (kind == AttachmentKind.Contact && transferState == AttachmentTransferState.Downloaded && absolutePath is not null)
        {
            var contact = await TryParseContactCardAsync(absolutePath, cancellationToken).ConfigureAwait(false);
            if (contact is not null)
            {
                return new ContactAttachment
                {
                    Id = id,
                    Kind = AttachmentKind.Contact,
                    FileName = fileName,
                    MimeType = row.MimeType,
                    SizeInBytes = row.TotalBytes,
                    TransferState = transferState,
                    IsSticker = row.IsSticker,
                    CreatedAt = createdAt,
                    Contact = contact,
                };
            }
        }

        return new MediaAttachment
        {
            Id = id,
            Kind = kind,
            FileName = fileName,
            MimeType = row.MimeType,
            SizeInBytes = row.TotalBytes,
            TransferState = transferState,
            IsSticker = row.IsSticker,
            CreatedAt = createdAt,
        };
    }

    private static async Task<ContactCard?> TryParseContactCardAsync(string absolutePath, CancellationToken cancellationToken)
    {
        try
        {
            var text = await File.ReadAllTextAsync(absolutePath, cancellationToken).ConfigureAwait(false);
            return VCardParser.Parse(text);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static AttachmentKind DetermineKind(AttachmentRow row)
    {
        if (row.IsSticker)
        {
            return AttachmentKind.Sticker;
        }

        var uti = row.UniformTypeIdentifier ?? string.Empty;
        var mime = row.MimeType ?? string.Empty;

        if (uti.Equals("public.vcard", StringComparison.OrdinalIgnoreCase)
            || (row.FileName?.EndsWith(".vcf", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return AttachmentKind.Contact;
        }

        if (mime.Equals("image/gif", StringComparison.OrdinalIgnoreCase))
        {
            return AttachmentKind.Gif;
        }

        if (mime.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            return AttachmentKind.Image;
        }

        if (mime.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
        {
            return AttachmentKind.Video;
        }

        if (mime.Equals("application/pdf", StringComparison.OrdinalIgnoreCase))
        {
            return AttachmentKind.Pdf;
        }

        if (mime.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)
            || uti.Contains("audio", StringComparison.OrdinalIgnoreCase))
        {
            var isVoiceMessage = row.TransferName?.StartsWith("Audio Message", StringComparison.OrdinalIgnoreCase) ?? false;
            return isVoiceMessage ? AttachmentKind.VoiceMessage : AttachmentKind.Audio;
        }

        return AttachmentKind.File;
    }

    private static AttachmentTransferState DetermineTransferState(AttachmentRow row, string? absolutePath)
    {
        if (absolutePath is not null && File.Exists(absolutePath))
        {
            return AttachmentTransferState.Downloaded;
        }

        return row.TotalBytes is > 0 ? AttachmentTransferState.Pending : AttachmentTransferState.Missing;
    }
}
