using System.Buffers.Binary;
using System.Text;

namespace iMessageSDK.Internal.AttributedBody;

/// <summary>
/// A reader for Apple's binary property list ("bplist00") format, sufficient to walk the object
/// graph macOS uses to archive an <c>NSAttributedString</c> via <c>NSKeyedArchiver</c>.
/// </summary>
/// <remarks>
/// This is a real, publicly documented binary format (unlike the semantic layout Apple gives
/// specific archived classes such as <c>NSAttributedString</c>, which is private and reverse
/// engineered on a best-effort basis by <see cref="AttributedBodyParser"/>). Objects are decoded
/// as plain .NET values: <see langword="null"/>, <see cref="bool"/>, <see cref="long"/>,
/// <see cref="double"/>, <see cref="DateTimeOffset"/>, <c>byte[]</c>, <see cref="string"/>,
/// <see cref="List{T}"/> of <see cref="object"/>, <see cref="Dictionary{TKey,TValue}"/> of
/// <see cref="string"/> to <see cref="object"/>, or <see cref="BplistUid"/> for an
/// unresolved object reference.
/// </remarks>
internal sealed class BinaryPlistReader
{
    private const int MaxNestingDepth = 64;

    private readonly byte[] _data;
    private readonly int _objectRefSize;
    private readonly int[] _objectOffsets;

    private BinaryPlistReader(byte[] data, int objectRefSize, int[] objectOffsets)
    {
        _data = data;
        _objectRefSize = objectRefSize;
        _objectOffsets = objectOffsets;
    }

    /// <summary>Attempts to parse a binary property list, returning its root object graph.</summary>
    public static bool TryParse(byte[] data, out object? root)
    {
        root = null;

        if (data.Length < 40 || !HasBplistMagic(data))
        {
            return false;
        }

        try
        {
            var trailer = data.AsSpan(data.Length - 32, 32);
            var offsetIntSize = trailer[6];
            var objectRefSize = trailer[7];
            var numObjects = checked((int)BinaryPrimitives.ReadUInt64BigEndian(trailer[8..16]));
            var topObjectIndex = checked((int)BinaryPrimitives.ReadUInt64BigEndian(trailer[16..24]));
            var offsetTableStart = checked((int)BinaryPrimitives.ReadUInt64BigEndian(trailer[24..32]));

            if (offsetIntSize is < 1 or > 8 || objectRefSize is < 1 or > 8 || numObjects <= 0)
            {
                return false;
            }

            var offsets = new int[numObjects];
            for (var i = 0; i < numObjects; i++)
            {
                offsets[i] = checked((int)ReadBigEndianUnsigned(data, offsetTableStart + (i * offsetIntSize), offsetIntSize));
            }

            var reader = new BinaryPlistReader(data, objectRefSize, offsets);
            root = reader.ReadObject(topObjectIndex, 0);
            return true;
        }
        catch (Exception ex) when (ex is IndexOutOfRangeException or ArgumentOutOfRangeException or OverflowException or InvalidOperationException)
        {
            return false;
        }
    }

    private static bool HasBplistMagic(byte[] data) =>
        data[0] == 'b' && data[1] == 'p' && data[2] == 'l' && data[3] == 'i'
        && data[4] == 's' && data[5] == 't' && data[6] == '0' && data[7] == '0';

    private object? ReadObject(int index, int depth)
    {
        if (depth > MaxNestingDepth)
        {
            throw new InvalidOperationException("Binary property list nesting exceeded the safety limit.");
        }

        if (index < 0 || index >= _objectOffsets.Length)
        {
            return null;
        }

        var offset = _objectOffsets[index];
        var marker = _data[offset];
        var type = (marker & 0xF0) >> 4;
        var info = marker & 0x0F;

        switch (type)
        {
            case 0x0:
                return info switch { 0x8 => false, 0x9 => true, _ => null };

            case 0x1:
                return ReadBigEndianUnsigned(_data, offset + 1, 1 << info);

            case 0x2:
            {
                var byteCount = 1 << info;
                return byteCount == 4
                    ? BinaryPrimitives.ReadSingleBigEndian(_data.AsSpan(offset + 1, 4))
                    : BinaryPrimitives.ReadDoubleBigEndian(_data.AsSpan(offset + 1, 8));
            }

            case 0x3:
            {
                var seconds = BinaryPrimitives.ReadDoubleBigEndian(_data.AsSpan(offset + 1, 8));
                return new DateTimeOffset(2001, 1, 1, 0, 0, 0, TimeSpan.Zero).AddSeconds(seconds);
            }

            case 0x4:
            {
                var (count, contentOffset) = ReadCount(info, offset + 1);
                return _data.AsSpan(contentOffset, count).ToArray();
            }

            case 0x5:
            {
                var (count, contentOffset) = ReadCount(info, offset + 1);
                return Encoding.ASCII.GetString(_data, contentOffset, count);
            }

            case 0x6:
            {
                var (count, contentOffset) = ReadCount(info, offset + 1);
                var chars = new char[count];
                for (var i = 0; i < count; i++)
                {
                    chars[i] = (char)((_data[contentOffset + (i * 2)] << 8) | _data[contentOffset + (i * 2) + 1]);
                }

                return new string(chars);
            }

            case 0x8:
                return new BplistUid((int)ReadBigEndianUnsigned(_data, offset + 1, info + 1));

            case 0xA:
            case 0xC:
            {
                var (count, refsOffset) = ReadCount(info, offset + 1);
                var list = new List<object?>(count);
                for (var i = 0; i < count; i++)
                {
                    var refIndex = (int)ReadBigEndianUnsigned(_data, refsOffset + (i * _objectRefSize), _objectRefSize);
                    list.Add(ReadObject(refIndex, depth + 1));
                }

                return list;
            }

            case 0xD:
            {
                var (count, keysOffset) = ReadCount(info, offset + 1);
                var valuesOffset = keysOffset + (count * _objectRefSize);
                var dict = new Dictionary<string, object?>(StringComparer.Ordinal);
                for (var i = 0; i < count; i++)
                {
                    var keyIndex = (int)ReadBigEndianUnsigned(_data, keysOffset + (i * _objectRefSize), _objectRefSize);
                    var valueIndex = (int)ReadBigEndianUnsigned(_data, valuesOffset + (i * _objectRefSize), _objectRefSize);
                    if (ReadObject(keyIndex, depth + 1) is string key)
                    {
                        dict[key] = ReadObject(valueIndex, depth + 1);
                    }
                }

                return dict;
            }

            default:
                return null;
        }
    }

    private (int Count, int ContentOffset) ReadCount(int info, int afterMarkerOffset)
    {
        if (info != 0xF)
        {
            return (info, afterMarkerOffset);
        }

        // Extended length: the next byte is an "int" object marker (0x1n) followed by its bytes.
        var intMarker = _data[afterMarkerOffset];
        var intByteCount = 1 << (intMarker & 0x0F);
        var count = (int)ReadBigEndianUnsigned(_data, afterMarkerOffset + 1, intByteCount);
        return (count, afterMarkerOffset + 1 + intByteCount);
    }

    private static long ReadBigEndianUnsigned(byte[] data, int offset, int byteCount)
    {
        long value = 0;
        for (var i = 0; i < byteCount; i++)
        {
            value = (value << 8) | data[offset + i];
        }

        return value;
    }
}
