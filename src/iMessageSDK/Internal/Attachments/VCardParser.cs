using iMessageSDK.Attachments;

namespace iMessageSDK.Internal.Attachments;

/// <summary>
/// A minimal parser for the vCard (.vcf) format Messages uses for shared contact cards, covering
/// the display name, phone numbers, and email addresses.
/// </summary>
internal static class VCardParser
{
    public static ContactCard? Parse(string vCardText)
    {
        string? displayName = null;
        var phoneNumbers = new List<string>();
        var emailAddresses = new List<string>();

        foreach (var rawLine in vCardText.Split('\n'))
        {
            var line = rawLine.TrimEnd('\r');
            if (line.Length == 0)
            {
                continue;
            }

            var colonIndex = line.IndexOf(':');
            if (colonIndex < 0)
            {
                continue;
            }

            var propertyAndParameters = line[..colonIndex];
            var value = line[(colonIndex + 1)..].Trim();
            var propertyName = propertyAndParameters.Split(';')[0].Trim();

            if (value.Length == 0)
            {
                continue;
            }

            switch (propertyName.ToUpperInvariant())
            {
                case "FN":
                    displayName ??= value;
                    break;
                case "TEL":
                    phoneNumbers.Add(value);
                    break;
                case "EMAIL":
                    emailAddresses.Add(value);
                    break;
            }
        }

        if (displayName is null && phoneNumbers.Count == 0 && emailAddresses.Count == 0)
        {
            return null;
        }

        return new ContactCard
        {
            DisplayName = displayName ?? phoneNumbers.FirstOrDefault() ?? emailAddresses.FirstOrDefault() ?? "Unknown Contact",
            PhoneNumbers = phoneNumbers,
            EmailAddresses = emailAddresses,
        };
    }
}
