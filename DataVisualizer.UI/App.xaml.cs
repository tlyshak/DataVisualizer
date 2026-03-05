using DataVisualizer.Application.Interfaces;
using DataVisualizer.Application.Services;
using DataVisualizer.Infrastructure.Mock;
using DataVisualizer.Infrastructure.Protocol;
using Microsoft.Extensions.DependencyInjection;
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
        var sc = new ServiceCollection();

        // Infrastructure
        sc.AddSingleton<ISignalProtocolParser, SignalProtocolParser>();
        sc.AddSingleton<ITcpSignalReceiver, MockByteStreamReceiver>();

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
