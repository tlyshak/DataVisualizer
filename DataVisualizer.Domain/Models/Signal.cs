namespace DataVisualizer.Domain.Models;

public readonly record struct Signal(
    DateTimeOffset CreatedOn,
    double FrequencyMHz,
    double BandwidthKHz,
    double SnrDb
);
