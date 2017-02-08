using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace Tests
{
    public class IdentityEnricher : ILogEventEnricher
    {
        private readonly HttpContext _context;
        public const string PropertyName = "User";

        public IdentityEnricher(HttpContext context)
        {
            _context = context;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            var userId = _context.User.GetUserId();
            if (userId != null)
            {
                logEvent.AddPropertyIfAbsent(new LogEventProperty(PropertyName, new ScalarValue(userId)));
            }
        }

    }

    public static class ClaimsPrincipalExtensions
    {
        public static string GetUserId(this ClaimsPrincipal principal)
        {
            return principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
    }
}