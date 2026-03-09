using DataVisualizer.Application.Interfaces;
using DataVisualizer.Domain.Models;
using DataVisualizer.Infrastructure.Protocol;
using DataVisualizer.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;

namespace DataVisualizer.Tests;

[TestClass]
public class SignalProtocolParserTests
{
    private static SignalProtocolParser CreateParser() => new(new Mock<ILogger<ISignalProtocolParser>>().Object, TestPacketFactory.SignalType); 

    [TestMethod]
    public void TryConsume_ReturnsFalse_WhenLessThanHeader()
    {
        var parser = CreateParser();

        ReadOnlySpan<byte> buf = [0x1C]; // 1 byte only
        bool ok = parser.TryConsume(ref buf, out Signal? signal);

        Assert.IsFalse(ok);
        Assert.IsNull(signal);
        Assert.AreEqual(1, buf.Length);
    }

    [TestMethod]
    public void TryConsume_ReturnsFalse_WhenNotEnoughForFullFrame()
    {
        var parser = CreateParser();

        byte[] partial = new byte[2 + 10];
        partial[0] = 0x1C; // len=28
        partial[1] = 0x01; // type=1

        ReadOnlySpan<byte> buf = partial;
        bool ok = parser.TryConsume(ref buf, out Signal? signal);

        Assert.IsFalse(ok);
        Assert.IsNull(signal);
        Assert.AreEqual(partial.Length, buf.Length);
    }

    [TestMethod]
    public void TryConsume_ConsumesFrame_AndReturnsNull_WhenTypeIsNotSignalType()
    {
        var parser = CreateParser();

        byte[] payload = new byte[28];
        byte[] frame = TestPacketFactory.BuildFrame(type: 2, payload);

        ReadOnlySpan<byte> buf = frame;
        bool ok = parser.TryConsume(ref buf, out Signal? signal);

        Assert.IsTrue(ok);
        Assert.IsNull(signal);
        Assert.AreEqual(0, buf.Length);
    }

    [TestMethod]
    public void TryConsume_ConsumesFrame_AndReturnsNull_WhenPayloadLengthIsNot28()
    {
        var parser = CreateParser();

        byte[] payload = new byte[10];
        byte[] frame = TestPacketFactory.BuildFrame(TestPacketFactory.SignalType, payload);

        ReadOnlySpan<byte> buf = frame;
        bool ok = parser.TryConsume(ref buf, out Signal? signal);

        Assert.IsTrue(ok);
        Assert.IsNull(signal);
        Assert.AreEqual(0, buf.Length);
    }

    [TestMethod]
    public void TryConsume_ParsesValidSignalFrame()
    {
        var parser = CreateParser();

        ulong tsMs = 1_700_000_000_000;
        ulong freqHz = 103_600_000;
        uint bwHz = 12_500;
        double snrDb = 12.34;

        byte[] payload = TestPacketFactory.BuildSignalPayload28(tsMs, freqHz, bwHz, snrDb);
        byte[] frame = TestPacketFactory.BuildFrame(TestPacketFactory.SignalType, payload);

        ReadOnlySpan<byte> buf = frame;
        bool ok = parser.TryConsume(ref buf, out Signal? signal);
        const double eps = 1e-6;

        Assert.IsTrue(ok);
        Assert.IsNotNull(signal);
        Assert.AreEqual(0, buf.Length);

        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds((long)tsMs), signal!.Value.CreatedOn);
        Assert.AreEqual(freqHz / 1_000_000.0, signal.Value.FrequencyMHz, eps);
        Assert.AreEqual(bwHz / 1_000.0, signal.Value.BandwidthKHz, eps);
        Assert.AreEqual(snrDb, signal.Value.SnrDb, eps);
    }

    [TestMethod]
    public void TryConsume_CanParseTwoFramesBackToBack()
    {
        var parser = CreateParser();

        byte[] p1 = TestPacketFactory.BuildSignalPayload28(1000, 100_000_000, 10_000, 1.0);
        byte[] p2 = TestPacketFactory.BuildSignalPayload28(2000, 101_000_000, 11_000, 2.0);

        byte[] f1 = TestPacketFactory.BuildFrame(TestPacketFactory.SignalType, p1);
        byte[] f2 = TestPacketFactory.BuildFrame(TestPacketFactory.SignalType, p2);

        byte[] combined = new byte[f1.Length + f2.Length];
        Buffer.BlockCopy(f1, 0, combined, 0, f1.Length);
        Buffer.BlockCopy(f2, 0, combined, f1.Length, f2.Length);

        ReadOnlySpan<byte> buf = combined;
        const double eps = 1e-6;

        Assert.IsTrue(parser.TryConsume(ref buf, out Signal? s1));
        Assert.IsNotNull(s1);

        Assert.IsTrue(parser.TryConsume(ref buf, out Signal? s2));
        Assert.IsNotNull(s2);

        Assert.AreEqual(0, buf.Length);
        Assert.AreEqual(100.0, s1!.Value.FrequencyMHz, eps);
        Assert.AreEqual(101.0, s2!.Value.FrequencyMHz, eps);
    }
}