using Serilog.Events;
using System;

namespace NotikaIdentityEmail.Logging
{
    public static class LogFilter
    {
        private const string SourceContextPropertyName = "SourceContext";
        private const string RequestPathPropertyName = "RequestPath";

        private static readonly string[] StaticExtensions =
        {
            ".js",
            ".css",
            ".map",
            ".woff",
            ".woff2",
            ".png",
            ".jpg",
            ".jpeg",
            ".gif",
            ".svg",
            ".ico"
        };

        public static bool ShouldExclude(LogEvent logEvent)
        {
            if (logEvent.Properties.TryGetValue(SourceContextPropertyName, out var sourceContext) &&
                sourceContext.ToString().Contains("Serilog.AspNetCore.RequestLoggingMiddleware", StringComparison.Ordinal))
            {
                return true;
            }

            if (!logEvent.Properties.TryGetValue(RequestPathPropertyName, out var requestPath))
            {
                return false;
            }

            var requestPathValue = requestPath.ToString().Trim('"');

            foreach (var extension in StaticExtensions)
            {
                if (requestPathValue.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}