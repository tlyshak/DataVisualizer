using DataVisualizer.Domain.Models;

namespace DataVisualizer.Application.Interfaces;

public interface ITcpSignalReceiver
{
    Task RunAsync(Func<Signal, Task> onEvent, CancellationToken ct);
}