using System;
using Microsoft.AspNetCore.Builder;

namespace PerformanceLog
{
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
}