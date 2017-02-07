using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Enrichers;
using Serilog.Events;

namespace Tests
{
    public class OperationEnricher : ILogEventEnricher
    {
        public const string OperationUserIdPropertyName = "User";

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            //logEvent.AddPropertyIfAbsent(new LogEventProperty(ThreadIdUserIdPropertyName, new ScalarValue("user1")));
        }

    }

    public class IdentityEnricher : ILogEventEnricher
    {
        public const string UserIdPropertyName = "User";

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            logEvent.AddPropertyIfAbsent(new LogEventProperty(UserIdPropertyName, new ScalarValue("user1")));
        }

    }

    public static class ThreadLoggerConfigurationIdentityEnricherExtensions
    {
        public static LoggerConfiguration WithIdentity(
            this LoggerEnrichmentConfiguration enrichmentConfiguration)
        {
            if (enrichmentConfiguration == null) throw new ArgumentNullException(nameof(enrichmentConfiguration));
            return enrichmentConfiguration.With<IdentityEnricher>();
        }
    }

    public class CorrelationIdMiddleware
    {
        private string _header;
        private RequestDelegate _next;

        private Microsoft.Extensions.Logging.ILogger _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger, string header)
        {
            this._next = next;
            this._header = header;
            this._logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            string correlationId = context.TraceIdentifier;
            StringValues correlationHeader = new StringValues(correlationId);
            if (context.Request.Headers.TryGetValue(this._header, out correlationHeader))
            {
                context.TraceIdentifier = correlationHeader.ToString();
            }
            await _next(context);
        }

    }
}