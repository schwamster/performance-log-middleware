using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;
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
                Duration = stopwatch.ElapsedMilliseconds,
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

    public static class PerformanceLogMiddlewareExtension
    {
        public static IApplicationBuilder UsePerformanceLog(this IApplicationBuilder app, PerformanceLogOptions options)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            return app.UseMiddleware<PerformanceLogMiddleware>(options);
        }

        public static IApplicationBuilder UsePerformanceLog(this IApplicationBuilder app, Action<IPerformanceLogOptions> optionsAction)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            if (optionsAction == null)
                throw new ArgumentNullException(nameof(optionsAction));
            var options = new PerformanceLogOptions();

            optionsAction.Invoke(options);

            return app.UseMiddleware<PerformanceLogMiddleware>(options);
        }
    }

    public interface IPerformanceLogOptions
    {
        IOptions Configure();
        void Default();
    }

    public interface IOptions
    {
        IOptions WithLogLevel(LogLevel logLevel);
        IOptions WithFormat(string format);
    }

    public class PerformanceLogOptions : IPerformanceLogOptions, IOptions
    {
        public LogLevel LogLevel { get; set; }

        public string Format { get; set; }

        public PerformanceLogOptions()
        {
            Default();
        }

        public void Default()
        {
            LogLevel = LogLevel.Information;
            Format = "request to {Operation} took {Duration}ms";
        }

        public IOptions WithFormat(string format)
        {
            this.Format = format;
            return this;
        }

        public IOptions Configure()
        {
            return this;
        }

        public IOptions WithLogLevel(LogLevel logLevel)
        {
            this.LogLevel = logLevel;
            return this;
        }

    }



    public class LogItem
    {
        public long Duration { get; set; }
        public string Operation { get; set; }

        public string CorrelationId { get; set; }

        public override string ToString()
        {
            return $"Operation: {Operation}; Duration: {Duration}; CorrelationId: {CorrelationId}";
        }
    }


}