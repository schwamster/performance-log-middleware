using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;

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
                 _logger.LogInformation($"Performance Begin: {DateTime.UtcNow} {context.Request.Path}");
                await _next(context);
                _logger.LogInformation($"Performance End: {DateTime.UtcNow} {context.Request.Path}");
        }
    }

    public static class PerformanceLogMiddlewareExtension
    {
        public static IApplicationBuilder UsePerformanceLog(this IApplicationBuilder builder, PerformanceLogOptions options)
        {
            return builder.UseMiddleware<PerformanceLogMiddleware>(options);
        }
    }

    public class PerformanceLogOptions
    {
        public PerformanceLogOptions()
        {

        }
    }
}