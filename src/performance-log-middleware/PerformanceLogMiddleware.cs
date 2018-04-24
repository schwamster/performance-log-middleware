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

        public PerformanceLogMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, PerformanceLogOptions options)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger("performance");
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

            switch (_options.LogLevel)
            {
                case LogLevel.Information:
                    _logger.LogInformation(_options.Format, logEntry.Operation, logEntry.Duration, logEntry.CorrelationId);
                    break;
                case LogLevel.Debug:
                    _logger.LogDebug(_options.Format, logEntry.Operation, logEntry.Duration, logEntry.CorrelationId);
                    break;
                case LogLevel.Warning:
                    _logger.LogWarning(_options.Format, logEntry.Operation, logEntry.Duration, logEntry.CorrelationId);
                    break;
                case LogLevel.Critical:
                    _logger.LogCritical(_options.Format, logEntry.Operation, logEntry.Duration, logEntry.CorrelationId);
                    break;
                case LogLevel.Error:
                    _logger.LogError(_options.Format, logEntry.Operation, logEntry.Duration, logEntry.CorrelationId);
                    break;
                case LogLevel.Trace:
                    _logger.LogTrace(_options.Format, logEntry.Operation, logEntry.Duration, logEntry.CorrelationId);
                    break;
                 case LogLevel.None:
                    _logger.LogInformation(_options.Format, logEntry.Operation, logEntry.Duration, logEntry.CorrelationId);
                    break;
            }


        }
    }
}