using System.Text;
using iMessageSDK.Messages;

namespace iMessageSDK.Internal.AttributedBody;

/// <summary>
/// Recovers plain text from the private, version-dependent archive format Messages stores in a
/// row's <c>attributedBody</c> column.
/// </summary>
/// <remarks>
/// macOS has used at least two different archive formats for this column across releases: a
/// binary property list produced by <c>NSKeyedArchiver</c>, and the older <c>NSArchiver</c>
/// "typedstream" format. Neither format's semantic layout for <c>NSAttributedString</c> is
/// publicly documented, so recovery here is deliberately conservative: it reliably recovers the
/// plain text, and does not attempt to recover run-level styling, links, or mentions, since doing
/// so correctly would require reverse-engineered assumptions that cannot be validated without a
/// corpus of real samples. <see cref="AttributedText.Runs"/> is therefore always empty for text
/// recovered this way.
/// </remarks>
internal static class AttributedBodyParser
{
    private static readonly HashSet<string> NonContentStrings = new(StringComparer.Ordinal)
    {
        "$null", "$class", "$classname", "$classes", "$archiver", "$top", "$objects", "$version",
        "NSObject", "NSString", "NSMutableString", "NSAttributedString", "NSMutableAttributedString",
        "NSDictionary", "NSMutableDictionary", "NSArray", "NSMutableArray", "NSNumber", "NSValue",
        "NSURL", "NSData", "NSMutableData", "NSDate",
    };

    public static AttributedText? TryParse(byte[]? attributedBody)
    {
        if (attributedBody is null || attributedBody.Length == 0)
        {
            return null;
        }

        var plainText = TryExtractFromKeyedArchiverBplist(attributedBody) ?? TryExtractFromTypedStream(attributedBody);
        return plainText is { Length: > 0 } ? new AttributedText { PlainText = plainText, Runs = [] } : null;
    }

    private static string? TryExtractFromKeyedArchiverBplist(byte[] data)
    {
        if (!BinaryPlistReader.TryParse(data, out var root)
            || root is not Dictionary<string, object?> archive
            || archive.GetValueOrDefault("$objects") is not List<object?> objects)
        {
            return null;
        }

        string? best = null;
        foreach (var candidate in objects)
        {
            if (candidate is string text
                && text.Length > 0
                && !NonContentStrings.Contains(text)
                && !text.StartsWith("__kIM", StringComparison.Ordinal)
                && !text.StartsWith("NS.", StringComparison.Ordinal)
                && (best is null || text.Length > best.Length))
            {
                best = text;
            }
        }

        return best;
    }

    private static string? TryExtractFromTypedStream(byte[] data)
    {
        var markerIndex = IndexOf(data, "NSString"u8, 0);
        var markerLength = "NSString"u8.Length;
        if (markerIndex < 0)
        {
            markerIndex = IndexOf(data, "NSMutableString"u8, 0);
            markerLength = "NSMutableString"u8.Length;
        }

        if (markerIndex < 0)
        {
            return null;
        }

        // The class name marker is followed, within a short distance, by a length-prefixed run of
        // the actual string content; scan a small window rather than assuming one fixed offset,
        // since the intervening type-encoding bytes vary. A short, low-value byte can coincidentally
        // look like a valid tiny length prefix, so the longest plausible candidate in the window is
        // preferred over the first one found.
        var searchStart = markerIndex + markerLength;
        var searchEnd = Math.Min(data.Length, searchStart + 32);

        string? best = null;
        for (var candidateOffset = searchStart; candidateOffset < searchEnd; candidateOffset++)
        {
            if (TryReadLengthPrefixedString(data, candidateOffset, out var text) && (best is null || text!.Length > best.Length))
            {
                best = text;
            }
        }

        return best;
    }

    private static bool TryReadLengthPrefixedString(byte[] data, int offset, out string? text)
    {
        text = null;

        int length;
        int contentOffset;
        var lengthMarker = data[offset];

        if (lengthMarker == 0x81 && offset + 2 < data.Length)
        {
            length = data[offset + 1] | (data[offset + 2] << 8);
            contentOffset = offset + 3;
        }
        else if (lengthMarker is >= 1 and <= 200)
        {
            length = lengthMarker;
            contentOffset = offset + 1;
        }
        else
        {
            return false;
        }

        if (length is < 1 or > 4096 || contentOffset + length > data.Length)
        {
            return false;
        }

        var candidate = data.AsSpan(contentOffset, length);
        if (!LooksLikePrintableUtf8(candidate))
        {
            return false;
        }

        text = Encoding.UTF8.GetString(candidate);
        return true;
    }

    private static bool LooksLikePrintableUtf8(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0)
        {
            return false;
        }

        var decoded = Encoding.UTF8.GetString(bytes);
        var validCount = decoded.Count(c => c != '�' && (!char.IsControl(c) || char.IsWhiteSpace(c)));
        return validCount / (double)decoded.Length >= 0.9;
    }

    private static int IndexOf(byte[] haystack, ReadOnlySpan<byte> needle, int startIndex)
    {
        for (var i = startIndex; i <= haystack.Length - needle.Length; i++)
        {
            if (haystack.AsSpan(i, needle.Length).SequenceEqual(needle))
            {
                return i;
            }
        }

        return -1;
    }
}
