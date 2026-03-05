namespace DataVisualizer.Domain.Models;

public sealed class SignalRecord
{
    private readonly List<Signal> _signals = [];
    public DateTimeOffset CreatedOn { get; }
    public double FrequencyMHz { get; }
    public double BandwidthKHz { get; }
    public double SnrDb { get; }
    public int Count => _signals.Count;
    public double MedianFrequency => _signals.OrderBy(s => s.FrequencyMHz).ElementAt(_signals.Count / 2).FrequencyMHz;

    public SignalRecord(Signal signal)
    {
        CreatedOn = signal.CreatedOn;
        FrequencyMHz = signal.FrequencyMHz;
        BandwidthKHz = signal.BandwidthKHz;
        SnrDb = signal.SnrDb;

        Add(signal);
    }

    public void Add(Signal signal)
    {
        _signals.Add(signal);
    }

    public bool ContainsFrequency(double freqMHz)
    {
        double halfBwMHz = (BandwidthKHz / 1000.0) / 2.0;
        return freqMHz >= FrequencyMHz - halfBwMHz && freqMHz <= FrequencyMHz + halfBwMHz;
    }
}