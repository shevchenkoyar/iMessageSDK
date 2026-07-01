using System.Text;

namespace iMessageSDK.Tests.Fixtures;

/// <summary>
/// A minimal, test-only binary property list writer, used to build small, correctness-verified
/// bplist00 byte streams to exercise <c>BinaryPlistReader</c> without depending on real chat.db
/// samples.
/// </summary>
public static class BplistTestWriter
{
    public static byte[] BuildStringArray(IReadOnlyList<string> values)
    {
        var objects = values.Select(EncodeAsciiString).ToList();
        var arrayMarker = (byte)(0xA0 | objects.Count);
        var arrayBytes = new[] { arrayMarker }.Concat(Enumerable.Range(0, objects.Count).Select(i => (byte)i)).ToArray();
        objects.Add(arrayBytes);
        return Build(objects, topObjectIndex: objects.Count - 1);
    }

    /// <summary>
    /// Builds a keyed-archiver-shaped envelope: a root dictionary with a single <c>$objects</c>
    /// key pointing at an array of strings, matching the shape <c>AttributedBodyParser</c> looks
    /// for.
    /// </summary>
    public static byte[] BuildObjectsEnvelope(IReadOnlyList<string> objectStrings)
    {
        var objects = objectStrings.Select(EncodeAsciiString).ToList();

        var arrayMarker = (byte)(0xA0 | objects.Count);
        var arrayBytes = new[] { arrayMarker }.Concat(Enumerable.Range(0, objects.Count).Select(i => (byte)i)).ToArray();
        var arrayIndex = objects.Count;
        objects.Add(arrayBytes);

        var objectsKeyIndex = objects.Count;
        objects.Add(EncodeAsciiString("$objects"));

        var dictBytes = new byte[] { 0xD1 }
            .Concat([(byte)objectsKeyIndex])
            .Concat([(byte)arrayIndex])
            .ToArray();
        objects.Add(dictBytes);

        return Build(objects, topObjectIndex: objects.Count - 1);
    }

    public static byte[] BuildDictionary(IReadOnlyDictionary<string, object> values)
    {
        var objects = new List<byte[]>();
        var keyIndices = new List<int>();
        var valueIndices = new List<int>();

        foreach (var (key, value) in values)
        {
            keyIndices.Add(objects.Count);
            objects.Add(EncodeAsciiString(key));

            valueIndices.Add(objects.Count);
            objects.Add(value switch
            {
                string s => EncodeAsciiString(s),
                bool b => [(byte)(b ? 0x09 : 0x08)],
                long l => Concat([0x13], WriteBigEndian(l, 8)),
                _ => throw new NotSupportedException($"Unsupported test value type {value.GetType()}"),
            });
        }

        var dictMarker = (byte)(0xD0 | values.Count);
        var dictBytes = new List<byte> { dictMarker };
        dictBytes.AddRange(keyIndices.Select(i => (byte)i));
        dictBytes.AddRange(valueIndices.Select(i => (byte)i));
        objects.Add(dictBytes.ToArray());

        return Build(objects, topObjectIndex: objects.Count - 1);
    }

    private static byte[] EncodeAsciiString(string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        if (bytes.Length < 0x0F)
        {
            return Concat([(byte)(0x50 | bytes.Length)], bytes);
        }

        // Extended length: marker with info=0xF, followed by an 8-byte "int" object encoding the
        // real length, then the content.
        var marker = new byte[] { 0x5F, 0x13 };
        return Concat(Concat(marker, WriteBigEndian(bytes.Length, 8)), bytes);
    }

    private static byte[] Build(IReadOnlyList<byte[]> objects, int topObjectIndex)
    {
        const int headerSize = 8; // "bplist00"

        var body = new List<byte>();
        var offsets = new List<int>();
        foreach (var obj in objects)
        {
            // Offsets in the offset table are absolute from the start of the file, not relative
            // to the start of the object body.
            offsets.Add(headerSize + body.Count);
            body.AddRange(obj);
        }

        const int offsetIntSize = 4;
        const int objectRefSize = 1;
        var offsetTableStart = 8 + body.Count;

        var result = new List<byte>();
        result.AddRange("bplist00"u8.ToArray());
        result.AddRange(body);
        foreach (var offset in offsets)
        {
            result.AddRange(WriteBigEndian(offset, offsetIntSize));
        }

        result.AddRange(new byte[5]);
        result.Add(0);
        result.Add(offsetIntSize);
        result.Add(objectRefSize);
        result.AddRange(WriteBigEndian(objects.Count, 8));
        result.AddRange(WriteBigEndian(topObjectIndex, 8));
        result.AddRange(WriteBigEndian(offsetTableStart, 8));

        return result.ToArray();
    }

    private static byte[] WriteBigEndian(long value, int byteCount)
    {
        var bytes = new byte[byteCount];
        for (var i = byteCount - 1; i >= 0; i--)
        {
            bytes[i] = (byte)(value & 0xFF);
            value >>= 8;
        }

        return bytes;
    }

    private static byte[] Concat(byte[] a, byte[] b) => [.. a, .. b];
}
