using Amazon.XRay.Recorder.Core;
using ApplicationIntegrationPatterns.Core.Services;
using AWS.Lambda.Powertools.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationIntegrationPatterns.Implementations
{
    internal class LoggingService : ILoggingService
    {
        public void LogInfo(string message) {
            Logger.AppendKey("TraceParent", Activity.Current.TraceId.ToString());
            Logger.AppendKey("Span", Activity.Current.SpanId.ToString());
            Logger.LogInformation(message);
        }

        public void LogWarning(Exception ex) {
            Logger.AppendKey("TraceParent", Activity.Current.TraceId.ToString());
            Logger.AppendKey("Span", Activity.Current.SpanId.ToString());
            Logger.LogWarning(ex);
        }

        public void LogWarning(string message) {
            Logger.AppendKey("TraceParent", Activity.Current.TraceId.ToString());
            Logger.AppendKey("Span", Activity.Current.SpanId.ToString());
            Logger.LogWarning(message);
        }

        public void LogWarning(Exception ex, string message) {
            Logger.AppendKey("TraceParent", Activity.Current.TraceId.ToString());
            Logger.AppendKey("Span", Activity.Current.SpanId.ToString());
            Logger.LogWarning(ex, message);
        }

        public void LogError(Exception ex) {
            Logger.AppendKey("TraceParent", Activity.Current.TraceId.ToString());
            Logger.AppendKey("Span", Activity.Current.SpanId.ToString());
            Logger.LogError(ex);
        } 

        public void LogError(string message) {
            Logger.AppendKey("TraceParent", Activity.Current.TraceId.ToString());
            Logger.AppendKey("Span", Activity.Current.SpanId.ToString());
            Logger.LogError(message);
        }

        public void LogError(Exception ex, string message) {
            Logger.AppendKey("TraceParent", Activity.Current.TraceId.ToString());
            Logger.AppendKey("Span", Activity.Current.SpanId.ToString());
            Logger.LogError(ex, message);
        }
    }
}
