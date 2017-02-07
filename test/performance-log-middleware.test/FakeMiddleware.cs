using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Tests
{
    public class FakeMiddleware
    {
        private TimeSpan _delay;
        private RequestDelegate _next;

        private Microsoft.Extensions.Logging.ILogger _logger;

        public FakeMiddleware(RequestDelegate next, ILoggerFactory loggerFactory, TimeSpan delay)
        {
            this._next = next;
            this._delay = delay;
            this._logger = loggerFactory.CreateLogger("something");
        }

        public async Task Invoke(HttpContext context)
        {
            _logger.LogInformation("FakeMiddleware has been called");

            _logger.LogInformation("User is not allowed to view resource {resource} because it is {reason}", "someresource", "christmas");

            System.Threading.Thread.Sleep(_delay);

            if (context.Request.Path.StartsWithSegments("/throw"))
            {
                throw new InvalidOperationException("expected exception");
            }
            await _next(context);
        }

    }
}