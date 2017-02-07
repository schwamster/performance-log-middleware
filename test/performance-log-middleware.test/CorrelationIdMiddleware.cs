using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace Tests
{
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