namespace iMessageSDK.Attachments;

/// <summary>
/// The structured content of a shared contact card.
/// </summary>
public sealed record ContactCard
{
    /// <summary>The contact's display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>The contact's phone numbers.</summary>
    public IReadOnlyList<string> PhoneNumbers { get; init; } = [];

    /// <summary>The contact's email addresses.</summary>
    public IReadOnlyList<string> EmailAddresses { get; init; } = [];
}
