using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Tests
{
    public class SerilogEnricherMiddleware
    {
       
        public const string OperationPropertyName = "Operation";
        private readonly RequestDelegate _next;

        public SerilogEnricherMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            using (LogContext.PushProperties(new OperationEnricher(context), new IdentityEnricher(context)))
            {
                await _next.Invoke(context);
            }
        }

    }
}