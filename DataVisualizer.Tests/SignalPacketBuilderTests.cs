using DataVisualizer.Infrastructure.Mock;

namespace DataVisualizer.Tests;

[TestClass]
public class SignalPacketBuilderTests
{
    [TestMethod]
    public void GenerateSignalPacket_ReturnsFrame_WithExpectedLength()
    {
        byte[] frame = SignalPacketBuilder.GenerateSignalPacket();

        Assert.IsNotNull(frame);
        Assert.HasCount(30, frame);
    }

    [TestMethod]
    public void GenerateSignalPacket_HeaderEncodesTypeAndPayloadLength()
    {
        byte[] frame = SignalPacketBuilder.GenerateSignalPacket();

        ushort header = (ushort)(frame[0] | (frame[1] << 8));

        int lengthLsb = header & 0xFF;
        int type = (header >> 8) & 0x7;
        int lengthMsb = (header >> 11) & 0x1F;

        int payloadLenght = (lengthMsb << 8) | lengthLsb;

        Assert.AreEqual(28, payloadLenght, "Payload length encoded in header must be 28.");
        Assert.AreEqual(SignalPacketBuilder.SignalType, type, "Type encoded in header must match SignalType.");
    }

    [TestMethod]
    public void GenerateSignalPacket_PayloadCanBeDecoded_AndLooksSane()
    {
        byte[] frame = SignalPacketBuilder.GenerateSignalPacket();

        ulong tsMs = BitConverter.ToUInt64(frame, 2 + 0);
        ulong freqHz = BitConverter.ToUInt64(frame, 2 + 8);
        uint bwHz = BitConverter.ToUInt32(frame, 2 + 16);
        double snrDb = BitConverter.ToDouble(frame, 2 + 20);

        long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        long diff = Math.Abs(nowMs - (long)tsMs);
        Assert.IsLessThan(10_000, diff, $"Timestamp too far from now. diffMs={diff}");

        Assert.IsTrue(
            bwHz == 12_500u || bwHz == 108_230u || bwHz == 25_000u,
            $"Unexpected bandwidth Hz: {bwHz}");

        Assert.IsTrue(snrDb >= 0.0 && snrDb < 30.0, $"SNR out of expected range: {snrDb}");

        Assert.IsGreaterThan(0u, freqHz, $"Frequency must be positive: {freqHz}");
    }

    [TestMethod]
    public void GenerateSignalPacket_FrequencyIsWithinBandwidthRange_OfSomeBase()
    {
        byte[] frame = SignalPacketBuilder.GenerateSignalPacket();

        ulong freqHz = BitConverter.ToUInt64(frame, 2 + 8);
        uint bwHz = BitConverter.ToUInt32(frame, 2 + 16);

        double freqMHz = freqHz / 1_000_000.0;
        double bwMHz = bwHz / 1_000_000.0;
        double halfBwMHz = bwMHz / 2.0;

        const double eps = 1e-6;

        bool matches =
            IsWithin(freqMHz, 103.6, halfBwMHz + eps) ||
            IsWithin(freqMHz, 104.2, halfBwMHz + eps) ||
            IsWithin(freqMHz, 99.8, halfBwMHz + eps);

        Assert.IsTrue(matches,
            $"Frequency {freqMHz:F6} MHz not within ±BW/2 of any base center. BWHz={bwHz}");
    }

    private static bool IsWithin(double value, double center, double halfWidth)
        => Math.Abs(value - center) <= halfWidth;
}
