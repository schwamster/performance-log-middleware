using System;
using Microsoft.AspNetCore.Builder;

namespace Tests
{
    public static class SerilogEnricherMiddlewareExtension
    {
        public static IApplicationBuilder UseSerilogEnricherMiddleware(this IApplicationBuilder app)
        {
            if (app == null)
                throw new ArgumentNullException(nameof(app));

            return app.UseMiddleware<SerilogEnricherMiddleware>();
        }

    }
}