namespace iMessageSDK.Internal.Database;

/// <summary>
/// Converts between .NET <see cref="DateTimeOffset"/> values and the "Mac absolute time" integers
/// Apple stores timestamp columns as (seconds, or since macOS Big Sur, nanoseconds, since
/// 2001-01-01 UTC).
/// </summary>
internal static class AppleTimeConverter
{
    private static readonly DateTimeOffset AppleEpoch = new(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // A nanosecond timestamp for any date since ~2001 is on the order of 10^17-10^18, while a
    // seconds timestamp for any realistic date is on the order of 10^8-10^9. This threshold
    // reliably distinguishes the two encodings without needing to know the exact OS version that
    // wrote the row.
    private const long NanosecondMagnitudeThreshold = 1_000_000_000_000;

    /// <summary>Converts a raw Apple timestamp column value to a <see cref="DateTimeOffset"/>, or <see langword="null"/> for an absent/zero value.</summary>
    public static DateTimeOffset? ToDateTimeOffset(long? rawValue)
    {
        if (rawValue is null or 0)
        {
            return null;
        }

        var seconds = rawValue.Value > NanosecondMagnitudeThreshold
            ? rawValue.Value / 1_000_000_000.0
            : rawValue.Value;

        return AppleEpoch.AddSeconds(seconds);
    }

    /// <summary>Converts a <see cref="DateTimeOffset"/> to the nanosecond-based raw column encoding used by modern macOS (Big Sur and later).</summary>
    public static long ToAppleNanoseconds(DateTimeOffset value) =>
        (long)((value - AppleEpoch).TotalSeconds * 1_000_000_000);
}
