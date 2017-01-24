using System;
using HealthCheck;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Tests
{
    public class Tests
    {
        [Fact]
        public void Test1() 
        {
            var loggerFactory = new Moq.Mock<ILoggerFactory>();
            var healthCheck = new HealthCheckMiddleware(null, loggerFactory.Object, new HealthCheckOptions());
            Assert.NotNull(healthCheck);
        }
    }
}
