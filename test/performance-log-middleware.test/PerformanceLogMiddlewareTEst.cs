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
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Tests
{
    public class TestLogger : ILogger<PerformanceLogMiddleware>
    {
        public List<Tuple<LogLevel, string>> Logs { get; set; }

        public TestLogger()
        {
            Logs = new List<Tuple<LogLevel, string>>();
        }

        public LogItem LogItem { get; set; }

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
            var logItem = state as LogItem;
            if (logItem != null)
            {
                this.LogItem = logItem;
            }
            this.Logs.Add(new Tuple<LogLevel, string>(logLevel, formatter(state, exception)));
        }
    }

    public class FakeMiddleware
    {
        private TimeSpan _delay;
        private RequestDelegate _next;

        public FakeMiddleware(RequestDelegate next, TimeSpan delay)
        {
            this._next = next;
            this._delay = delay;
        }

        public async Task Invoke(HttpContext context)
        {

            System.Threading.Thread.Sleep(_delay);

            if (context.Request.Path.StartsWithSegments("/throw"))
            {
                throw new InvalidOperationException("expected exception");
            }
            await _next(context);
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


        private Mock<ILoggerFactory> GetLoggerFactory(ILogger logger)
        {
            var loggerFactory = new Mock<ILoggerFactory>();
            loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(logger);

            return loggerFactory;
        }

        [Fact]
        public async void InvokeTest_GivenDelay_LogsDuration()
        {
            //Arrange
            var logger = new TestLogger();

            var builder = new WebHostBuilder()
                .ConfigureServices(app =>
                   {
                       app.AddSingleton<ILoggerFactory>(GetLoggerFactory(logger).Object);
                   }
                )
                .Configure(app =>
                {
                    app.UsePerformanceLog(options => options.Default());
                    app.UseMiddleware<FakeMiddleware>(TimeSpan.FromMilliseconds(20));
                });

            var server = new TestServer(builder);

            //Act 
            var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "/delay/");
            var responseMessage = await server.CreateClient().SendAsync(requestMessage);

            //Assert
            logger.Logs.Exists(x => x.Item1 == LogLevel.Information && x.Item2.StartsWith("Performance")).Should().BeTrue();
            logger.LogItem.Duration.Should().BeInRange(20, 30);
        }

        //TODO: needs to be fixed. Otherwise logs of failing calls will be ignored
        //[Fact]
        //public async void InvokeTest_GivenExceptionInStack_LogsDuration()
        //{
        //    //Arrange
        //    var logger = new TestLogger();

        //    var builder = new WebHostBuilder()
        //        .ConfigureServices(app =>
        //        {
        //            app.AddSingleton<ILoggerFactory>(GetLoggerFactory(logger).Object);
        //        }
        //        )
        //        .Configure(app =>
        //        {
        //            app.UsePerformanceLog(options => options.Default());
        //            app.UseMiddleware<FakeMiddleware>(TimeSpan.FromMilliseconds(20));
        //        });

        //    var server = new TestServer(builder);

        //    //Act
        //    var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "/throw/");
        //    try
        //    {
        //        var responseMessage = await server.CreateClient().SendAsync(requestMessage);
        //    }
        //    catch
        //    {
        //        //catch the clients exception
        //    }

        //    logger.Logs.Exists(x => x.Item1 == LogLevel.Information && x.Item2.StartsWith("Performance")).Should().BeTrue();
        //    logger.LogItem.Duration.Should().BeInRange(20, 30);
        //}

        [Fact]
        public async void InvokeTest_GivenNoLoggerConfigured_ThrowsNoException()
        {
            //Arrange
            var builder = new WebHostBuilder()
                .Configure(app =>
                {
                    app.UsePerformanceLog(options => options.Default());
                    app.UseMiddleware<FakeMiddleware>(TimeSpan.FromMilliseconds(20));
                });

            var server = new TestServer(builder);

            //Act 
            var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "/delay/");
            var responseMessage = await server.CreateClient().SendAsync(requestMessage);
        }
    }
}
