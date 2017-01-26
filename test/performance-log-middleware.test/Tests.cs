using System;
using PerformanceLog;
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
            var performanceLog = new PerformanceLogMiddleware(null, loggerFactory.Object, new PerformanceLogOptions());
            Assert.NotNull(performanceLog);
        }
    }
}
