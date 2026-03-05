using DataVisualizer.Domain.Models;

namespace DataVisualizer.Application.Interfaces;

public interface ISignalProtocolParser
{
    bool TryConsume(ref ReadOnlySpan<byte> buffer, out Signal? signal);
}
