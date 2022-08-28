using Amazon.XRay.Recorder.Core;
using ApplicationIntegrationPatterns.Core.Services;
using AWS.Lambda.Powertools.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationIntegrationPatterns.Implementations
{
    internal class LoggingService : ILoggingService
    {
        public void LogInfo(string message) => Logger.LogInformation(message);

        public void LogWarning(Exception ex) => Logger.LogWarning(ex);

        public void LogWarning(string message) => Logger.LogWarning(message);

        public void LogWarning(Exception ex, string message) => Logger.LogWarning(ex, message);

        public void LogError(Exception ex) => Logger.LogError(ex);

        public void LogError(string message) => Logger.LogError(message);

        public void LogError(Exception ex, string message) => Logger.LogError(ex, message);
    }
}
