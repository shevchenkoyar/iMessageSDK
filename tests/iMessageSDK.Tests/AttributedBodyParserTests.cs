using System.Text;
using iMessageSDK.Internal.AttributedBody;
using iMessageSDK.Tests.Fixtures;

namespace iMessageSDK.Tests;

public class AttributedBodyParserTests
{
    [Fact]
    public void TryParse_ReturnsNull_ForNullOrEmptyInput()
    {
        Assert.Null(AttributedBodyParser.TryParse(null));
        Assert.Null(AttributedBodyParser.TryParse([]));
    }

    [Fact]
    public void TryParse_RecoversPlainText_FromKeyedArchiverBplist()
    {
        var bytes = BplistTestWriter.BuildObjectsEnvelope([
            "$null",
            "NSMutableAttributedString",
            "NSObject",
            "NSString",
            "NSDictionary",
            "This is the real message text",
        ]);

        var result = AttributedBodyParser.TryParse(bytes);

        Assert.NotNull(result);
        Assert.Equal("This is the real message text", result.PlainText);
        Assert.Empty(result.Runs);
    }

    [Fact]
    public void TryParse_RecoversPlainText_FromLegacyTypedStreamFormat()
    {
        var content = "Hi from the legacy format"u8.ToArray();
        var bytes = new List<byte>();
        bytes.AddRange("streamtyped"u8.ToArray());
        bytes.AddRange(Encoding.ASCII.GetBytes("NSMutableString"));
        bytes.Add(0x01); // a couple of filler type-encoding bytes before the length-prefixed text
        bytes.Add(0x2B);
        bytes.Add((byte)content.Length);
        bytes.AddRange(content);

        var result = AttributedBodyParser.TryParse(bytes.ToArray());

        Assert.NotNull(result);
        Assert.Equal("Hi from the legacy format", result.PlainText);
    }

    [Fact]
    public void TryParse_ReturnsNull_ForUnrecognizedData()
    {
        var result = AttributedBodyParser.TryParse([1, 2, 3, 4, 5]);

        Assert.Null(result);
    }
}
