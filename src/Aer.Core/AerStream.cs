using System.Buffers.Binary;

namespace Aer;

/// <summary>Length-prefixed AER-B frames for streaming transports.</summary>
public static class AerStream
{
    private static readonly byte[] Magic = "AERF"u8.ToArray();

    public static byte[] EncodeFrame(AerValue value)
    {
        var payload = AerBinary.Encode(value);
        var frame = new byte[9 + payload.Length];
        Magic.CopyTo(frame, 0);
        frame[4] = 1;
        BinaryPrimitives.WriteUInt32LittleEndian(frame.AsSpan(5, 4), checked((uint)payload.Length));
        payload.CopyTo(frame, 9);
        return frame;
    }

    public static IReadOnlyList<AerValue> DecodeFrames(ReadOnlySpan<byte> data, int maxFrameBytes = 16 * 1024 * 1024)
    {
        var results = new List<AerValue>();
        var offset = 0;
        while (offset < data.Length)
        {
            if (data.Length - offset < 9) throw new AerFormatException("AER008", "Truncated AER frame header.");
            if (!data.Slice(offset, 4).SequenceEqual(Magic)) throw new AerFormatException("AER002", "Invalid AER frame magic.");
            if (data[offset + 4] != 1) throw new AerFormatException("AER009", "Unsupported AER frame version.");
            var length = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(offset + 5, 4));
            if (length > maxFrameBytes) throw new AerFormatException("AER006", "AER frame exceeds configured size limit.");
            if (data.Length - offset - 9 < length) throw new AerFormatException("AER008", "Truncated AER frame payload.");
            results.Add(AerBinary.Decode(data.Slice(offset + 9, checked((int)length))));
            offset += 9 + checked((int)length);
        }
        return results;
    }
}
