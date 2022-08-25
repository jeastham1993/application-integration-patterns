using SynchronousApi.Core.Services;
using Serilog;
using Serilog.Core;
using Serilog.Formatting.Compact;

namespace SynchronousApi.Implementations;

public class SerilogLogger : ILoggingService
{
    private readonly Logger _log;
    private string _traceId;

    public SerilogLogger()
    {
        _log = new LoggerConfiguration()
            .WriteTo.Console(new RenderedCompactJsonFormatter())
            .CreateLogger();
    }

    public void AddTraceId(string traceId)
    {
        this._traceId = traceId;
    }

    public void LogInfo(string message)
    {
        this._log.ForContext("TraceId", this._traceId).Information(message);
    }
}