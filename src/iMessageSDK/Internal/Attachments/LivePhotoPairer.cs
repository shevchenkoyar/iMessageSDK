using iMessageSDK.Attachments;

namespace iMessageSDK.Internal.Attachments;

/// <summary>
/// Recognizes Apple Live Photo pairs among a message's attachments: a still image and a short
/// motion video sharing the same base file name, and folds each pair into a single
/// <see cref="LivePhotoAttachment"/>.
/// </summary>
internal static class LivePhotoPairer
{
    public static IReadOnlyList<Attachment> Pair(IReadOnlyList<Attachment> attachments)
    {
        var images = attachments.OfType<MediaAttachment>().Where(a => a.Kind == AttachmentKind.Image).ToList();
        var videosByBaseName = attachments.OfType<MediaAttachment>()
            .Where(a => a.Kind == AttachmentKind.Video)
            .GroupBy(a => GetBaseName(a.FileName), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Key.Length > 0)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        if (videosByBaseName.Count == 0 || images.Count == 0)
        {
            return attachments;
        }

        var pairedVideoIdByImageId = new Dictionary<AttachmentId, AttachmentId>();
        var consumedVideoIds = new HashSet<AttachmentId>();
        foreach (var image in images)
        {
            var baseName = GetBaseName(image.FileName);
            if (baseName.Length > 0 && videosByBaseName.TryGetValue(baseName, out var video))
            {
                pairedVideoIdByImageId[image.Id] = video.Id;
                consumedVideoIds.Add(video.Id);
            }
        }

        if (pairedVideoIdByImageId.Count == 0)
        {
            return attachments;
        }

        var result = new List<Attachment>(attachments.Count);
        foreach (var attachment in attachments)
        {
            if (attachment is MediaAttachment media)
            {
                if (pairedVideoIdByImageId.TryGetValue(media.Id, out var videoId))
                {
                    result.Add(new LivePhotoAttachment
                    {
                        Id = media.Id,
                        Kind = AttachmentKind.LivePhoto,
                        FileName = media.FileName,
                        MimeType = media.MimeType,
                        SizeInBytes = media.SizeInBytes,
                        TransferState = media.TransferState,
                        IsSticker = media.IsSticker,
                        CreatedAt = media.CreatedAt,
                        MotionVideoAttachmentId = videoId,
                    });
                    continue;
                }

                if (consumedVideoIds.Contains(media.Id))
                {
                    // Folded into the LivePhotoAttachment above; the video remains individually
                    // retrievable via its own AttachmentId, it is just not listed twice.
                    continue;
                }
            }

            result.Add(attachment);
        }

        return result;
    }

    private static string GetBaseName(string? fileName) =>
        fileName is null ? string.Empty : Path.GetFileNameWithoutExtension(fileName);
}
