using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Builder;

namespace HealthCheck
{
    public class HealthCheckMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;
        private readonly HealthCheckOptions _options;

        public HealthCheckMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, HealthCheckOptions options)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<HealthCheckMiddleware>();
            _options = options;
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Path.StartsWithSegments(_options.Path))
            {
                _logger.LogInformation("Healthcheck requested: " + context.Request.Path);
                await context.Response.WriteAsync(_options.Message);
            }
            else
            {
                await _next(context);
            }
        }
    }

    public static class HealthCheckMiddlewareExtension
    {
        public static IApplicationBuilder UseHealthcheckEndpoint(this IApplicationBuilder builder, HealthCheckOptions options)
        {
            return builder.UseMiddleware<HealthCheckMiddleware>(options);
        }
    }

    public class HealthCheckOptions
    {
        public HealthCheckOptions()
        {
            Path = "healthcheck";
            Message = "i am alive!";    
        }

        public string Path { get; set; }
        public string Message { get; set; }
    }
}