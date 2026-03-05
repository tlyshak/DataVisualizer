using DataVisualizer.Application.Interfaces;
using DataVisualizer.Domain.Models;
using DataVisualizer.Infrastructure.Protocol;

namespace DataVisualizer.Infrastructure.Mock;

public sealed class MockByteStreamReceiver : ITcpSignalReceiver
{
    private readonly SignalProtocolParser _parser = new();

    public async Task RunAsync(Func<Signal, Task> onSignalCreated, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                byte[] packet = SignalPacketBuilder.GenerateSignalPacket();

                ReadOnlySpan<byte> span = packet.AsSpan();
                _parser.TryConsume(ref span, out Signal? parsed);

                if (parsed is Signal signal)
                    await onSignalCreated(signal);

                await Task.Delay(500, ct);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }
}
