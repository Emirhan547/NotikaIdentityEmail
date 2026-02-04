using Serilog.Core;
using Serilog.Events;

namespace NotikaIdentityEmail.Logging
{
    public class SeverityEnricher : ILogEventEnricher
    {
        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var severity = logEvent.Level.ToString();
            var property = propertyFactory.CreateProperty("Severity", severity);
            logEvent.AddPropertyIfAbsent(property);
        }
    }
}