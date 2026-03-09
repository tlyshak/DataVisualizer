using DataVisualizer.Application.Interfaces;
using DataVisualizer.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DataVisualizer.Infrastructure.Mock;

public sealed class MockByteStreamReceiver(ILogger<ITcpSignalReceiver> logger, ISignalProtocolParser parser) : ITcpSignalReceiver
{
    private readonly ISignalProtocolParser _parser = parser;
    private readonly ILogger<ITcpSignalReceiver> _logger = logger;

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
            _logger.LogInformation("Receiving signals was canceled.");
        }
    }
}
