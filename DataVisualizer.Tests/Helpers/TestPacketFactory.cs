namespace DataVisualizer.Tests.Helpers;

internal static class TestPacketFactory
{
    public const int SignalType = 1;
    public static byte[] BuildFrame(int type, byte[] payload)
    {
        int payloadLen = payload.Length;

        int lenLsb = payloadLen & 0xFF;
        int lenMsb = (payloadLen >> 8) & 0x1F;

        ushort header = (ushort)(lenLsb | (type << 8) | (lenMsb << 11));

        byte[] frame = new byte[2 + payloadLen];

        frame[0] = (byte)header;
        frame[1] = (byte)(header >> 8);

        Array.Copy(payload, 0, frame, 2, payloadLen);

        return frame;
    }

    public static byte[] BuildSignalPayload28(ulong tsMs, ulong freqHz, uint bwHz, double snrDb)
    {
        byte[] payload = new byte[28];

        Array.Copy(BitConverter.GetBytes(tsMs), 0, payload, 0, 8);
        Array.Copy(BitConverter.GetBytes(freqHz), 0, payload, 8, 8);
        Array.Copy(BitConverter.GetBytes(bwHz), 0, payload, 16, 4);
        Array.Copy(BitConverter.GetBytes(snrDb), 0, payload, 20, 8);

        return payload;
    }
}
