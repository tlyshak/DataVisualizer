using DataVisualizer.Application.Interfaces;
using DataVisualizer.Domain.Models;

namespace DataVisualizer.Application.Services;

public sealed class SignalProcessingService(ITcpSignalReceiver receiver, IAggregationService aggregation) : ISignalProcessingService
{
    private readonly ITcpSignalReceiver _receiver = receiver;
    private readonly IAggregationService _aggregation = aggregation;

    public Task StartAsync(Action<SignalRecord> onRecordUpdated, CancellationToken ct)
    {
        return _receiver.RunAsync(signal =>
        {
            var record = _aggregation.AddOrUpdate(signal);
            onRecordUpdated(record);
            return Task.CompletedTask;
        }, ct);
    }
}
