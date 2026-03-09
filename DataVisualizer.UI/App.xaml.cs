using DataVisualizer.Application.Interfaces;
using DataVisualizer.Application.Services;
using DataVisualizer.Infrastructure.Mock;
using DataVisualizer.Infrastructure.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using SignalMonitor.Wpf.ViewModels;
using System.Windows;

namespace DataVisualizer.UI;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        Log.Logger = new LoggerConfiguration()
            .WriteTo.File(
                "Logs/log.txt",
                rollingInterval: RollingInterval.Day)
            .CreateLogger();

        var sc = new ServiceCollection();
        sc.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });

        // Infrastructure
        sc.AddSingleton<ISignalProtocolParser, SignalProtocolParser>();
        sc.AddSingleton<ITcpSignalReceiver, MockByteStreamReceiver>();
        sc.AddSingleton<ISignalProtocolParser, SignalProtocolParser>();

        // Application
        sc.AddSingleton<IAggregationService, AggregationService>();
        sc.AddSingleton<ISignalProcessingService, SignalProcessingService>();

        // UI
        sc.AddSingleton<MainViewModel>();

        Services = sc.BuildServiceProvider();

        var window = new MainWindow
        {
            DataContext = Services.GetRequiredService<MainViewModel>()
        };
        window.Show();

        base.OnStartup(e);
    }
}
