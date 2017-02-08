using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Tests
{
    public class OperationEnricher : ILogEventEnricher
    {
        private readonly HttpContext _context;
        public const string PropertyName = "Operation";

        public OperationEnricher(HttpContext context)
        {
            _context = context;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(new LogEventProperty(PropertyName, new ScalarValue(_context.Request.Path)));
        }

    }
}