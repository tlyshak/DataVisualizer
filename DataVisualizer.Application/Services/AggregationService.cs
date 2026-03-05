using DataVisualizer.Application.Interfaces;
using DataVisualizer.Domain.Models;

namespace DataVisualizer.Application.Services;

public sealed class AggregationService : IAggregationService
{
    private readonly List<SignalRecord> _records = [];
    private readonly Lock _lock = new();

    public IReadOnlyList<SignalRecord> Records
    {
        get { lock (_lock) return [.. _records]; }
    }

    public SignalRecord AddOrUpdate(Signal signal)
    {
        lock (_lock)
        {
            var existing = _records.FirstOrDefault(r => r.ContainsFrequency(signal.FrequencyMHz));
            if (existing != null)
            {
                existing.Add(signal);
                return existing;
            }

            var newRecord = new SignalRecord(signal);
            _records.Add(newRecord);
            return newRecord;
        }
    }

    public void Clear()
    {
        lock (_lock) _records.Clear();
    }
}