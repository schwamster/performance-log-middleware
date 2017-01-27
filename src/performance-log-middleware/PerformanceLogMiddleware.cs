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
            _logger = loggerFactory.CreateLogger<PerformanceLogMiddleware>();
            _options = options;
        }

        public async Task Invoke(HttpContext context)
        {
            var correlationId = context.TraceIdentifier; //Guid.NewGuid().ToString();
            
            
            var stopwatch = Stopwatch.StartNew();
            await _next(context);
            var logEntry = new LogItem
            {
                Duration = stopwatch.ElapsedMilliseconds,
                Operation = context.Request.Path,
                CorrelationId = correlationId
            };

            _logger.Log(_options.LogLevel, new EventId(), logEntry, null, _options.Formatter);
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

        public static IApplicationBuilder UsePerformanceLog(this IApplicationBuilder app, Action<IPerformanceLogOptions> options)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

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
        IOptions WithFormatter(Func<LogItem, Exception, string> formatter);
    }

    public class PerformanceLogOptions : IPerformanceLogOptions, IOptions
    {
        public LogLevel LogLevel { get; set; }

        public Func<LogItem, Exception, string> Formatter { get; set; }

        public PerformanceLogOptions()
        {
            Default();
        }

        public void Default()
        {
            LogLevel = LogLevel.Information;
            Formatter = (log, exception) => { return $"{log}"; };
        }

        public IOptions WithFormatter(Func<LogItem, Exception, string> formatter)
        {
            this.Formatter = formatter;
            return this;
        }

        public IOptions Configure()
        {
            return this;
        }

        IOptions IOptions.WithLogLevel(LogLevel logLevel)
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