using DataVisualizer.Domain.Models;

namespace DataVisualizer.Application.Interfaces;

public interface IAggregationService
{
    IReadOnlyList<SignalRecord> Records { get; }
    SignalRecord AddOrUpdate(Signal ev);
    void Clear();
}

