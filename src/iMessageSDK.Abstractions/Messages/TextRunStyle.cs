namespace iMessageSDK.Messages;

/// <summary>
/// Text styling flags that can be applied to a <see cref="TextRun"/>.
/// </summary>
[Flags]
public enum TextRunStyle
{
    /// <summary>No styling.</summary>
    None = 0,

    /// <summary>Bold text.</summary>
    Bold = 1 << 0,

    /// <summary>Italic text.</summary>
    Italic = 1 << 1,

    /// <summary>Strikethrough text.</summary>
    Strikethrough = 1 << 2,

    /// <summary>Underlined text.</summary>
    Underline = 1 << 3,
}
