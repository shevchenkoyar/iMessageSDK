namespace iMessageSDK.Attachments;

/// <summary>
/// A strongly-typed, immutable identifier for an <see cref="Attachment"/>.
/// </summary>
/// <remarks>
/// Wraps the stable identifier Messages assigns to an attachment, not any storage-specific row
/// number. Two <see cref="AttachmentId"/> values are equal if and only if they wrap the same
/// underlying value.
/// </remarks>
/// <param name="Value">The underlying identifier value.</param>
public readonly record struct AttachmentId(string Value)
{
    /// <summary>Returns the underlying identifier value.</summary>
    public override string ToString() => Value;
}
