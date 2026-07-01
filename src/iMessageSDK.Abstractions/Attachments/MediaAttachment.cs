namespace iMessageSDK.Attachments;

/// <summary>
/// An <see cref="Attachment"/> whose content is a media file: an image, video, audio recording,
/// voice message, PDF, plain file, GIF, or sticker.
/// </summary>
public sealed record MediaAttachment : Attachment
{
    /// <summary>The playback duration, for audio or video content.</summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>The pixel width, for image or video content.</summary>
    public int? Width { get; init; }

    /// <summary>The pixel height, for image or video content.</summary>
    public int? Height { get; init; }
}
