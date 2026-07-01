namespace iMessageSDK.Attachments;

/// <summary>
/// Categorizes the content of an <see cref="Attachment"/>.
/// </summary>
public enum AttachmentKind
{
    /// <summary>A still image.</summary>
    Image,

    /// <summary>A video clip.</summary>
    Video,

    /// <summary>An audio recording.</summary>
    Audio,

    /// <summary>A push-to-talk style voice message recorded in Messages.</summary>
    VoiceMessage,

    /// <summary>A PDF document.</summary>
    Pdf,

    /// <summary>A file that does not fall into any other more specific category.</summary>
    File,

    /// <summary>An animated GIF.</summary>
    Gif,

    /// <summary>A sticker, optionally overlaid on another attachment.</summary>
    Sticker,

    /// <summary>A shared contact card. See <see cref="ContactAttachment"/>.</summary>
    Contact,

    /// <summary>A shared location. See <see cref="LocationAttachment"/>.</summary>
    Location,

    /// <summary>An Apple Live Photo (a still image paired with a short motion video). See <see cref="LivePhotoAttachment"/>.</summary>
    LivePhoto,
}
