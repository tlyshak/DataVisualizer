using CommunityToolkit.Mvvm.ComponentModel;

namespace SignalMonitor.Wpf.ViewModels;

public partial class SignalRowViewModel(DateTimeOffset createdOn, double frequencyMHz, double bandwidthKHz, double snrDb) : ObservableObject
{
    public DateTimeOffset CreatedOn { get; } = createdOn;
    public double FrequencyMHz { get; } = frequencyMHz;
    public double BandwidthKHz { get; } = bandwidthKHz;
    public double SnrDb { get; } = snrDb;

    [ObservableProperty]
    private int count;
    [ObservableProperty]
    private double medianFrequency;
}