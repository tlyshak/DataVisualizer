using DataVisualizer.Domain.Models;

namespace DataVisualizer.Application.Interfaces;

public interface ISignalProcessingService
{
    Task StartAsync(Action<SignalRecord> onRecordUpdated, CancellationToken ct);
}