namespace ApplicationIntegrationPatterns.Core.Services;

public interface ILoggingService {
    void LogInfo(string message);

    void LogWarning(Exception ex);

    void LogWarning(string message);

    void LogWarning(Exception ex, string message);

    void LogError(Exception ex);

    void LogError(string message);

    void LogError(Exception ex, string message);
}