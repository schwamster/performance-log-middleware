using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace PerformanceLog
{
    public class PerformanceLogMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly PerformanceLogOptions _options;
        private readonly ILoggerFactory _loggerFactory;

        public PerformanceLogMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, PerformanceLogOptions options)
        {
            _next = next;
            _loggerFactory = loggerFactory;
            _options = options;
        }

        public async Task Invoke(HttpContext context)
        {
            var correlationId = context.TraceIdentifier;

            var stopwatch = Stopwatch.StartNew();
            await _next(context);
            var logEntry = new LogItem
            {
                Duration = stopwatch.Elapsed.TotalMilliseconds,
                Operation = context.Request.Path,
                CorrelationId = correlationId
            };

            context.Response.Headers.Add("X-Request-Duration", logEntry.Duration.ToString());

            var logger = _loggerFactory.CreateLogger("performance");
            switch (_options.LogLevel)
            {
                case LogLevel.Information:
                    logger.LogInformation(_options.Format, logEntry.Operation, logEntry.Duration, logEntry.CorrelationId);
                    break;
                case LogLevel.Debug:
                    logger.LogDebug(_options.Format, logEntry.Operation, logEntry.Duration, logEntry.CorrelationId);
                    break;
                case LogLevel.Warning:
                    logger.LogWarning(_options.Format, logEntry.Operation, logEntry.Duration, logEntry.CorrelationId);
                    break;
                case LogLevel.Critical:
                    logger.LogCritical(_options.Format, logEntry.Operation, logEntry.Duration, logEntry.CorrelationId);
                    break;
                case LogLevel.Error:
                    logger.LogError(_options.Format, logEntry.Operation, logEntry.Duration, logEntry.CorrelationId);
                    break;
                case LogLevel.Trace:
                    logger.LogTrace(_options.Format, logEntry.Operation, logEntry.Duration, logEntry.CorrelationId);
                    break;
                case LogLevel.None:
                    logger.LogInformation(_options.Format, logEntry.Operation, logEntry.Duration, logEntry.CorrelationId);
                    break;
            }


        }
    }
}