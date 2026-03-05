using DataVisualizer.Application.Interfaces;
using DataVisualizer.Domain.Models;

namespace DataVisualizer.Infrastructure.Protocol;

public sealed class SignalProtocolParser(int signalType = 1) : ISignalProtocolParser
{
    private readonly int _signalType = signalType;

    public bool TryConsume(ref ReadOnlySpan<byte> buffer, out Signal? signal)
    {
        signal = null;

        if (buffer.Length < 2)
            return false;

        ushort header = (ushort)(buffer[0] | (buffer[1] << 8));
        int payloadLenght = (((header >> 11) & 0x1F) << 8) | (header & 0xFF);
        int type = (header >> 8) & 0x7;

        int frameLenght = 2 + payloadLenght;
        if (buffer.Length < frameLenght)
            return false;

        var payload = buffer.Slice(2, payloadLenght);
        buffer = buffer.Slice(frameLenght);

        if (type != _signalType || payloadLenght != 28)
            return true;

        ulong tsMs = ReadUInt64(payload, 0);
        ulong freqHz = ReadUInt64(payload, 8);
        uint bwHz = ReadUInt32(payload, 16);
        double snr = ReadDouble(payload, 20);

        DateTimeOffset timestamp;
        try
        {
            timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)tsMs);
        }
        catch
        {
            return true;
        }

        double freqMHz = freqHz / 1000000.0;
        double bwKHz = bwHz / 1000.0;

        signal = new Signal(timestamp, freqMHz, bwKHz, snr);
        return true;
    }

    private static ulong ReadUInt64(ReadOnlySpan<byte> bytes, int offset)
    {
        var s = bytes.Slice(offset, 8);
        return BitConverter.ToUInt64(s);
    }

    private static uint ReadUInt32(ReadOnlySpan<byte> bytes, int offset)
    {
        var s = bytes.Slice(offset, 4);
        return BitConverter.ToUInt32(s);
    }

    private static double ReadDouble(ReadOnlySpan<byte> bytes, int offset)
    {
        var s = bytes.Slice(offset, 8);
        return BitConverter.ToDouble(s);
    }
}
