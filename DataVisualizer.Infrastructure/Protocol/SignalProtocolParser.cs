using DataVisualizer.Application.Interfaces;
using DataVisualizer.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DataVisualizer.Infrastructure.Protocol;

public sealed class SignalProtocolParser(ILogger<ISignalProtocolParser> logger, int signalType = 1) : ISignalProtocolParser
{
    private readonly int _signalType = signalType;
    private readonly ILogger<ISignalProtocolParser> _logger = logger;

    public bool TryConsume(ref ReadOnlySpan<byte> buffer, out Signal? signal)
    {
        _logger.LogDebug("TryConsume: buffer length = {Length}", buffer.Length);
        signal = null;

        if (buffer.Length < 2)
        {
            _logger.LogDebug("Not enough data for header. Buffer length = {Length}", buffer.Length);
            return false;
        }
        
        ushort header = (ushort)(buffer[0] | (buffer[1] << 8));
        int payloadLength = (((header >> 11) & 0x1F) << 8) | (header & 0xFF);
        int type = (header >> 8) & 0x7;

        int frameLength = 2 + payloadLength;
        if (buffer.Length < frameLength)
        {
            _logger.LogDebug("Incomplete frame. Needed = {FrameLength}, Available = {BufferLength}", frameLength, buffer.Length);
            return false;
        }

        var payload = buffer.Slice(2, payloadLength);
        buffer = buffer.Slice(frameLength);

        if (type != _signalType || payloadLength != 28)
        {
            _logger.LogDebug("Skipping frame. Type={Type}, PayloadLength={PayloadLength}", type, payloadLength);
            return true;
        }

        ulong tsMs = ReadUInt64(payload, 0);
        ulong freqHz = ReadUInt64(payload, 8);
        uint bwHz = ReadUInt32(payload, 16);
        double snr = ReadDouble(payload, 20);

        DateTimeOffset timestamp;
        try
        {
            timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)tsMs).ToLocalTime();
        }
        catch
        {
            _logger.LogWarning("Invalid timestamp received: {Timestamp}", tsMs);
            return true;
        }

        double freqMHz = freqHz / 1000000.0;
        double bwKHz = bwHz / 1000.0;

        signal = new Signal(timestamp, freqMHz, bwKHz, snr);
        _logger.LogInformation("Signal parsed: {Freq:F3} MHz | BW {BW:F2} kHz | SNR {SNR:F1} dB", signal.Value.FrequencyMHz, signal.Value.BandwidthKHz, signal.Value.SnrDb);
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
