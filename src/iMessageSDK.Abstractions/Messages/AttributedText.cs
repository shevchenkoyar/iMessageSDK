namespace iMessageSDK.Messages;

/// <summary>
/// Rich-text content recovered from a message's underlying attributed string, exposing
/// formatting, links, and mentions without leaking Apple's internal archive format.
/// </summary>
/// <remarks>
/// Recovery of <see cref="Runs"/> is best-effort: Apple's on-disk representation for rich text is
/// private and has changed across macOS releases. <see cref="PlainText"/> is always reliable;
/// treat an empty <see cref="Runs"/> collection as "no styling could be recovered", not
/// necessarily "no styling exists".
/// </remarks>
public sealed record AttributedText
{
    /// <summary>The plain-text content, with all formatting removed.</summary>
    public required string PlainText { get; init; }

    /// <summary>The formatting, link, and mention runs recovered for <see cref="PlainText"/>.</summary>
    public IReadOnlyList<TextRun> Runs { get; init; } = [];
}
