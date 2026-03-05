using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DataVisualizer.Application.Interfaces;
using DataVisualizer.Domain.Models;
using DataVisualizer.UI;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace SignalMonitor.Wpf.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISignalProcessingService _signalProcessingService;
    private readonly IAggregationService _aggregationService;
    private CancellationTokenSource? _cts;
    private readonly Dictionary<SignalRecord, SignalRowViewModel> _recordViewModels = [];
    private const string ZeroRecordsText = "Records: 0";
    private const string ProcessStopped = "Stopped";
    public ObservableCollection<SignalRowViewModel> Records { get; } = [];
    public ICollectionView RecordsView { get; }

    [ObservableProperty]
    private string frequencyContains = string.Empty;
    [ObservableProperty]
    private int minCount = 0;
    [ObservableProperty]
    private string statusText = ProcessStopped;
    [ObservableProperty]
    private string recordsSummary = ZeroRecordsText;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool isRunning;

    public MainViewModel(ISignalProcessingService signalProcessingService, IAggregationService aggregationService)
    {
        _signalProcessingService = signalProcessingService;
        _aggregationService = aggregationService;
        RecordsView = CollectionViewSource.GetDefaultView(Records);
        RecordsView.Filter = FilterRecord;
        RecordsView.SortDescriptions.Add(new SortDescription(nameof(SignalRowViewModel.CreatedOn), ListSortDirection.Descending));
    }

    [RelayCommand]
    public async Task StartAsync()
    {
        if (IsRunning) return;

        if (_cts != null)
            return;

        _cts = new CancellationTokenSource();
        IsRunning = true;

        StatusText = "Running";

        try
        {
            await _signalProcessingService.StartAsync(onRecordUpdated: OnRecordUpdated, _cts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            StatusText = "Error";

            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsRunning = false;
            StatusText = ProcessStopped;

            _cts = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    public void Stop()
    {
        _cts?.Cancel();
    }

    [RelayCommand(CanExecute = nameof(CanClear))]
    public void Clear()
    {
        Records.Clear();
        _recordViewModels.Clear();
        _aggregationService.Clear();

        RecordsSummary = ZeroRecordsText;

        ClearCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    public void ResetFilters()
    {
        FrequencyContains = string.Empty;
        MinCount = 0;
    }

    partial void OnFrequencyContainsChanged(string value) => RecordsView.Refresh();
    partial void OnMinCountChanged(int value) => RecordsView.Refresh();

    private bool FilterRecord(object obj)
    {
        if (obj is not SignalRowViewModel row)
            return false;

        if (MinCount > 0 && row.Count < MinCount)
            return false;

        if (!string.IsNullOrWhiteSpace(FrequencyContains))
        {
            if (!row.FrequencyMHz
                .ToString("F3")
                .Contains(FrequencyContains.Trim()))
                return false;
        }

        return true;
    }

    private bool CanStop()
    {
        return IsRunning;
    }

    private bool CanClear()
    {
        return Records.Count > 0;
    }

    private void OnRecordUpdated(SignalRecord record)
    {
        App.Current.Dispatcher.BeginInvoke(() =>
        {
            if (!_recordViewModels.TryGetValue(record, out var signalRow))
            {
                signalRow = new SignalRowViewModel(
                    record.CreatedOn,
                    record.FrequencyMHz,
                    record.BandwidthKHz,
                    record.SnrDb);

                _recordViewModels[record] = signalRow;

                Records.Add(signalRow);

                ClearCommand.NotifyCanExecuteChanged();
            }

            signalRow.Count = record.Count;
            signalRow.MedianFrequency = record.MedianFrequency;

            RecordsSummary = $"Records: {Records.Count}";
        });
    }
}