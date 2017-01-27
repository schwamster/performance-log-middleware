using System;
using System.Collections.Generic;
using System.Net.Http;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PerformanceLog;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests
{

    public class TestLogger: ILogger<PerformanceLogMiddleware>
    {
        public List<Tuple<LogLevel, string>> Logs { get; set; }

        public TestLogger()
        {
            Logs = new List<Tuple<LogLevel, string>>();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            this.Logs.Add(new Tuple<LogLevel, string>(logLevel, formatter(state, exception)));
        }
    }
    public class PerformanceLogMiddlewareTest
    {
        [Fact]
        public void InitializeMiddlewareTest()
        {
            var loggerFactory = new Moq.Mock<ILoggerFactory>();
            var performanceLog = new PerformanceLogMiddleware(null, loggerFactory.Object, new PerformanceLogOptions());
            Assert.NotNull(performanceLog);
        }


        private Mock<ILoggerFactory> GetLoggerFactory<T>(ILogger<T> logger)
        {
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(x => x.CreateLogger<T>()).Returns(logger);

            return loggerFactory;
        }

        [Fact]
        public async void InvokeTest()
        {
            //Arrange
            //Func<ILogger<PerformanceLogMiddleware>> logger = () =>
            //{
            //    var loggerMock =new Mock<Logger<PerformanceLogMiddleware>>();
            //    return loggerMock.Object;
            //};
            var logger = new TestLogger();

          
          
            var builder = new WebHostBuilder()
                .ConfigureServices(app =>
                    {
                        app.AddSingleton<ILoggerFactory>(GetLoggerFactory<PerformanceLogMiddleware>(logger).Object);
                    }
                )
                .Configure(app =>
                {
                    
                    app.UsePerformanceLog(options => options.Default());
                });

            var server = new TestServer(builder);

            //Act 
            var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "/something/");
            var responseMessage = await server.CreateClient().SendAsync(requestMessage);

            //Assert
            logger.Logs.Count.Should().BeGreaterThan(0);
        }
    }
}
