using iMessageSDK.Internal.Database;

namespace iMessageSDK.Tests;

public class AppleTimeConverterTests
{
    [Fact]
    public void ToDateTimeOffset_ReturnsNull_ForNullOrZero()
    {
        Assert.Null(AppleTimeConverter.ToDateTimeOffset(null));
        Assert.Null(AppleTimeConverter.ToDateTimeOffset(0));
    }

    [Fact]
    public void RoundTrips_NanosecondEncoding()
    {
        var original = new DateTimeOffset(2024, 3, 15, 9, 30, 0, TimeSpan.Zero);

        var nanoseconds = AppleTimeConverter.ToAppleNanoseconds(original);
        var roundTripped = AppleTimeConverter.ToDateTimeOffset(nanoseconds);

        Assert.NotNull(roundTripped);
        Assert.Equal(original, roundTripped.Value, TimeSpan.FromMilliseconds(1));
    }

    [Fact]
    public void ToDateTimeOffset_TreatsSmallMagnitudeValues_AsSeconds()
    {
        // A legacy (pre-Big Sur) row storing seconds-since-2001, not nanoseconds.
        var appleEpoch = new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var secondsSinceEpoch = 700_000_000L; // ~2023, well under the nanosecond magnitude threshold

        var result = AppleTimeConverter.ToDateTimeOffset(secondsSinceEpoch);

        Assert.NotNull(result);
        Assert.Equal(appleEpoch.AddSeconds(secondsSinceEpoch), result.Value);
    }
}
