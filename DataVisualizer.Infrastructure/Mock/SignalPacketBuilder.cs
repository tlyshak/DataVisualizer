namespace DataVisualizer.Infrastructure.Mock;

public static class SignalPacketBuilder
{
    private static readonly Random _rnd = new();
    public const int SignalType = 1;
    private const int PayloadLength = 28;

    private static readonly (double CenterMHz, double BandwidthKHz)[] Bases =
    {
        (103.6, 12.5),
        (104.2, 108.23),
        (99.8, 25.0),
    };

    public static byte[] GenerateSignalPacket()
    {
        var (centerMHz, bwKHz) = Bases[_rnd.Next(Bases.Length)];

        var timestamp = DateTimeOffset.UtcNow;
        double freqMHz = GenerateFrequencyWithinBandwidth(centerMHz, bwKHz);
        double snrDb = NextRange(0.0, 30.0);

        ulong tsMs = (ulong)timestamp.ToUnixTimeMilliseconds();
        ulong freqHz = ToHz(freqMHz);
        uint bwHz = ToHzFromKHz(bwKHz);

        var frame = new byte[2 + PayloadLength];
        WriteHeader(frame.AsSpan(0, 2), PayloadLength, SignalType);
        WritePayload(frame, 2, tsMs, freqHz, bwHz, snrDb);

        return frame;
    }

    private static double GenerateFrequencyWithinBandwidth(double centerMHz, double bwKHz)
    {
        double halfBwMHz = (bwKHz / 1000.0) / 2.0;
        double offset = NextRange(-1.0, 1.0) * (halfBwMHz * 0.99);

        return centerMHz + offset;
    }

    private static double NextRange(double minInclusive, double maxExclusive)
        => minInclusive + _rnd.NextDouble() * (maxExclusive - minInclusive);

    private static ulong ToHz(double MHz) => (ulong)Math.Round(MHz * 1000000);
    private static uint ToHzFromKHz(double kHz) => (uint)Math.Round(kHz * 1000);

    private static void WriteHeader(Span<byte> headerBytes, int payloadLen, int type)
    {
        // 8 bit length LSB | 3 bit type | 5 bit length MSB
        int lenLsb = payloadLen & 0xFF;
        int lenMsb = (payloadLen >> 8) & 0x1F;

        ushort header = (ushort)(lenLsb | (type << 8) | (lenMsb << 11));

        // lsb msb
        headerBytes[0] = (byte)header;
        headerBytes[1] = (byte)(header >> 8);
    }

    private static void WritePayload(byte[] frame, int offset, ulong tsMs, ulong freqHz, uint bwHz, double snrDb)
    {
        Array.Copy(BitConverter.GetBytes(tsMs), 0, frame, offset + 0, 8);
        Array.Copy(BitConverter.GetBytes(freqHz), 0, frame, offset + 8, 8);
        Array.Copy(BitConverter.GetBytes(bwHz), 0, frame, offset + 16, 4);
        Array.Copy(BitConverter.GetBytes(snrDb), 0, frame, offset + 20, 8);
    }
}