namespace SynchronousApi.Core.Services;

public interface ILoggingService {
    void LogInfo(string message);

    void AddTraceId(string traceId);
}