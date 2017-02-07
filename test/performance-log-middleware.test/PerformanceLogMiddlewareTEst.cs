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
using Serilog.Core;
using Serilog.Events;
using Serilog;
using System.IO;
using Serilog.Formatting.Json;
using Serilog.Filters;
using Serilog.Formatting.Compact;
using Serilog.Formatting;
using System.Text.RegularExpressions;
using System.Linq;

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
            System.Threading.Thread.Sleep(_delay);

            if (context.Request.Path.StartsWithSegments("/throw"))
            {
                throw new InvalidOperationException("expected exception");
            }
            await _next(context);
        }

    }

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
            // StringValues 
            // context.Request.Headers.TryGetValue(this._header, out correlationId)
            _logger.LogInformation("FakeMiddleware has been called");
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


        private Mock<ILoggerFactory> GetLoggerFactory(Microsoft.Extensions.Logging.ILogger logger)
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
            var logItem = logger.Logs.FirstOrDefault(x => x.Item1 == LogLevel.Information && x.Item2.StartsWith("request"));
            logItem.Should().NotBeNull();

            var duration = Regex.Match(logItem.Item2,"request to .* took ([0-9\\.]*)ms").Groups[1].Value;
            double.Parse(duration).Should().BeInRange(20, 30);
        }

        [Fact]
        public async void InvokeTest_GivenCustomFormat_LogsDuration()
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
                    app.UsePerformanceLog(options => options.Configure().WithFormat("customduration: {operation} => {duration}"));
                    app.UseMiddleware<FakeMiddleware>(TimeSpan.FromMilliseconds(20));
                });

            var server = new TestServer(builder);

            //Act 
            var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "/delay/");
            var responseMessage = await server.CreateClient().SendAsync(requestMessage);

            //Assert
            var logItem = logger.Logs.FirstOrDefault(x => x.Item1 == LogLevel.Information && x.Item2.StartsWith("customduration: "));
            logItem.Should().NotBeNull();
            var duration = Regex.Match(logItem.Item2,"customduration\\: .* => ([0-9\\.]*)").Groups[1].Value;
            double.Parse(duration).Should().BeInRange(20, 30);
        }

        [Fact]
        public async void InvokeTest_GivenCustomLogLevel_LogsDuration()
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
                    app.UsePerformanceLog(options => options.Configure().WithLogLevel(LogLevel.Trace));
                    app.UseMiddleware<FakeMiddleware>(TimeSpan.FromMilliseconds(20));
                });

            var server = new TestServer(builder);

            //Act 
            var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "/delay/");
            var responseMessage = await server.CreateClient().SendAsync(requestMessage);

            //Assert
            var logItem = logger.Logs.FirstOrDefault(x => x.Item1 == LogLevel.Trace && x.Item2.StartsWith("request"));
            logItem.Should().NotBeNull();

            var duration = Regex.Match(logItem.Item2,"request to .* took ([0-9\\.]*)ms").Groups[1].Value;
            double.Parse(duration).Should().BeInRange(20, 30);
        }


        [Fact]
        public async void InvokeTest_GivenExceptionInStack_DoesNotLog()
        {
            //this is not exactly the behaviour i wanted just want to make it clear with a unit test
            //the problem with this behaviour is, that no performance logs will exits for calls that fail somewhere down the line
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
            var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "/throw/");
            try
            {
                var responseMessage = await server.CreateClient().SendAsync(requestMessage);
            }
            catch
            {
                //catch the clients exception
            }
            logger.Logs.Exists(x => x.Item1 == LogLevel.Information && x.Item2.StartsWith("Performance")).Should().BeFalse();
        }


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

        [Fact, Trait("Category", "Usage")]
        public async void InvokeTest_TestWithDefaultLogger()
        {
            //Arrange
            var builder = new WebHostBuilder()
                .ConfigureServices(app =>
                {
                    app.AddLogging();
                }
                )
                .Configure((app) =>
                {
                    var loggingService = app.ApplicationServices.GetService<ILoggerFactory>();
                    loggingService.AddConsole(true);
                    loggingService.AddDebug();
                    app.UsePerformanceLog(options => options.Default());
                    app.UseMiddleware<FakeMiddleware>(TimeSpan.FromMilliseconds(20));
                });

            var server = new TestServer(builder);

            //Act 
            var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "/delay/");
            var responseMessage = await server.CreateClient().SendAsync(requestMessage);
        }

        public class CustomFormatter : ITextFormatter
        {
            public void Format(LogEvent logEvent, TextWriter output)
            {
                output.Write("something");
            }
        }

        [Fact, Trait("Category", "Usage")]
        public async void InvokeTest_TestWithSerilog()
        {
            var formatter = new CustomJsonFormatter("testapp");
            Serilog.Log.Logger = new Serilog.LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Logger(lc => lc
                .Filter.ByExcluding(Matching.FromSource("performance"))
                .WriteTo.RollingFile(formatter, Path.Combine("c:\\logs\\", "log-{Date}.txt"))
                .WriteTo.Console(formatter))
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(Matching.FromSource("performance"))
                .WriteTo.RollingFile(formatter, Path.Combine("c:\\logs\\", "log-perf-{Date}.txt"))
                .WriteTo.Console(formatter))
            .CreateLogger();            

            //Arrange
            var builder = new WebHostBuilder()
                .Configure((app) =>
                {
                    var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
                    loggerFactory.AddSerilog();
                    app.UsePerformanceLog(options => options.Configure().WithFormat("request to {operation} took {duration}ms {correlationId}"));
                    app.UseMiddleware<FakeMiddleware>(TimeSpan.FromMilliseconds(20));
                });

            var server = new TestServer(builder);

            //Act 
            var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "/delay/");
            var responseMessage = await server.CreateClient().SendAsync(requestMessage);

        }
    }
}
