using iMessageSDK.Internal.AttributedBody;
using iMessageSDK.Tests.Fixtures;

namespace iMessageSDK.Tests;

public class BinaryPlistReaderTests
{
    [Fact]
    public void TryParse_ReturnsFalse_ForNonBplistData()
    {
        var result = BinaryPlistReader.TryParse([1, 2, 3, 4], out var root);

        Assert.False(result);
        Assert.Null(root);
    }

    [Fact]
    public void TryParse_DecodesDictionaryWithStringIntAndBool()
    {
        var bytes = BplistTestWriter.BuildDictionary(new Dictionary<string, object>
        {
            ["greeting"] = "Hello, World!",
            ["count"] = 42L,
            ["flag"] = true,
        });

        var parsed = BinaryPlistReader.TryParse(bytes, out var root);

        Assert.True(parsed);
        var dict = Assert.IsType<Dictionary<string, object?>>(root);
        Assert.Equal("Hello, World!", dict["greeting"]);
        Assert.Equal(42L, dict["count"]);
        Assert.Equal(true, dict["flag"]);
    }

    [Fact]
    public void TryParse_DecodesArrayOfStrings()
    {
        var bytes = BplistTestWriter.BuildStringArray(["$null", "NSString", "Hello there"]);

        var parsed = BinaryPlistReader.TryParse(bytes, out var root);

        Assert.True(parsed);
        var list = Assert.IsType<List<object?>>(root);
        Assert.Equal(["$null", "NSString", "Hello there"], list);
    }
}
