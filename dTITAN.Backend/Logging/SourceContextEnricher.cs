using Serilog.Core;
using Serilog.Events;

namespace dTITAN.Backend.Logging;

public class SourceContextEnricher : ILogEventEnricher
{
    private const string PropertyName = "ShortSourceContext";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (logEvent.Properties.TryGetValue("SourceContext", out var value) && value is ScalarValue sv && sv.Value is string s)
        {
            // Keep last two segments if available, otherwise last one
            var parts = s.Split('.');
            string shortName;
            if (parts.Length >= 2)
            {
                shortName = string.Join('.', parts[^2..]);
            }
            else
            {
                shortName = s;
            }

            var prop = propertyFactory.CreateProperty(PropertyName, shortName);
            logEvent.AddPropertyIfAbsent(prop);
        }
    }
}
