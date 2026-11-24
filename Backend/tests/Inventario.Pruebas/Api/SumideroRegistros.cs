using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace Inventario.Pruebas.Api;

public sealed class SumideroRegistros : ILogEventSink
{
    private readonly ConcurrentQueue<LogEvent> _eventos = new();

    public IReadOnlyCollection<LogEvent> Eventos => _eventos;

    public void Emit(LogEvent logEvent) => _eventos.Enqueue(logEvent);
}
