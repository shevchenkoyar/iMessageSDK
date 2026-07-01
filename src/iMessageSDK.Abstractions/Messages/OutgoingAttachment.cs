namespace iMessageSDK.Messages;

/// <summary>
/// A local file to be sent as an attachment.
/// </summary>
/// <remarks>
/// Messages automation on macOS sends attachments by reference to a file already on disk; there
/// is no supported way to send directly from an in-memory stream. If your content originates in
/// memory, write it to a temporary file first and pass that path here.
/// </remarks>
public sealed record OutgoingAttachment
{
    /// <summary>The absolute path of the local file to send.</summary>
    public required string FilePath { get; init; }

    /// <summary>An optional file name to present instead of the source file's own name.</summary>
    public string? FileNameOverride { get; init; }

    /// <summary>Creates an <see cref="OutgoingAttachment"/> referencing a file already on disk.</summary>
    /// <param name="filePath">The absolute path of the local file to send.</param>
    /// <param name="fileNameOverride">An optional file name to present instead of the source file's own name.</param>
    public static OutgoingAttachment FromFile(string filePath, string? fileNameOverride = null) =>
        new() { FilePath = filePath, FileNameOverride = fileNameOverride };
}
