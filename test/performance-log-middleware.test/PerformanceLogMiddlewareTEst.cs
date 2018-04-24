using System;
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

            var duration = Regex.Match(logItem.Item2, "request to .* took ([0-9\\.]*)ms").Groups[1].Value;
            double.Parse(duration).Should().BeInRange(19, 30);
        }

        [Fact]
        public async void InvokeTest_GivenSubMsDelay_LogsDurationAccuratly()
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
                    app.UseMiddleware<FakeMiddleware>(TimeSpan.FromMilliseconds(0.2));
                });

            var server = new TestServer(builder);

            //Act 
            var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "/delay/");
            var responseMessage = await server.CreateClient().SendAsync(requestMessage);

            //Assert
            var logItem = logger.Logs.FirstOrDefault(x => x.Item1 == LogLevel.Information && x.Item2.StartsWith("request"));
            logItem.Should().NotBeNull();

            var duration = Regex.Match(logItem.Item2, "request to .* took ([0-9\\.]*)ms").Groups[1].Value;
            double.Parse(duration).Should().BeInRange(0.01, 0.3);
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
            var duration = Regex.Match(logItem.Item2, "customduration\\: .* => ([0-9\\.]*)").Groups[1].Value;
            double.Parse(duration).Should().BeInRange(19, 30);
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

            var duration = Regex.Match(logItem.Item2, "request to .* took ([0-9\\.]*)ms").Groups[1].Value;
            double.Parse(duration).Should().BeInRange(19, 30);
        }

        [Fact]
        public async void InvokeTest_GivenCustomLogLevelCritical_LogsDuration()
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
                    app.UsePerformanceLog(options => options.Configure().WithLogLevel(LogLevel.Critical));
                    app.UseMiddleware<FakeMiddleware>(TimeSpan.FromMilliseconds(20));
                });

            var server = new TestServer(builder);

            //Act 
            var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "/delay/");
            var responseMessage = await server.CreateClient().SendAsync(requestMessage);

            //Assert
            var logItem = logger.Logs.FirstOrDefault(x => x.Item1 == LogLevel.Critical && x.Item2.StartsWith("request"));
            logItem.Should().NotBeNull();

            var duration = Regex.Match(logItem.Item2, "request to .* took ([0-9\\.]*)ms").Groups[1].Value;
            double.Parse(duration).Should().BeInRange(19, 30);
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
                .WriteTo.RollingFile(formatter, Path.Combine(".\\logs\\", "log-{Date}.log"))
                .WriteTo.Console(formatter))
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(Matching.FromSource("performance"))
                .WriteTo.RollingFile(formatter, Path.Combine(".\\logs\\", "log-perf-{Date}.log"))
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

        [Fact, Trait("Category", "Usage")]
        public async void InvokeTest_TestWithSerilogAndCorrelationMiddleware()
        {
            var formatter = new CustomJsonFormatter("testapp");
            Serilog.Log.Logger = new Serilog.LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Logger(lc => lc
                .Filter.ByExcluding(Matching.FromSource("performance"))
                .WriteTo.RollingFile(formatter, Path.Combine(".\\logs\\", "log-{Date}.log"))
                .WriteTo.Console(formatter))
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(Matching.FromSource("performance"))
                .WriteTo.RollingFile(formatter, Path.Combine(".\\logs\\", "log-perf-{Date}.log"))
                .WriteTo.Console(formatter))
            .CreateLogger();

            //Arrange
            var builder = new WebHostBuilder()
                .Configure((app) =>
                {
                    var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
                    loggerFactory.AddSerilog();
                    app.UseMiddleware<CorrelationIdMiddleware>("X-Correlation-Id");
                    app.UsePerformanceLog(options => options.Configure().WithFormat("request to {operation} took {duration}ms"));
                    app.UseMiddleware<FakeMiddleware>(TimeSpan.FromMilliseconds(20));
                });

            var server = new TestServer(builder);

            //Act 
            var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "/delay/");
            requestMessage.Headers.TryAddWithoutValidation("X-Correlation-Id", "jonas");
            var responseMessage = await server.CreateClient().SendAsync(requestMessage);

        }

        [Fact, Trait("Category", "Usage")]
        public async void InvokeTest_TestWithSerilogAndCorrelationMiddlewareWithCriticalLogLevel()
        {
            
            var formatter = new CustomJsonFormatter("testapp");
            Serilog.Log.Logger = new Serilog.LoggerConfiguration()
            .MinimumLevel.Verbose()
            .WriteTo.Logger(lc => lc
                .Filter.ByExcluding(Matching.FromSource("performance"))
                .WriteTo.RollingFile(formatter, Path.Combine(".\\logs\\", "log-{Date}.log"))
                .WriteTo.Console(formatter))
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(Matching.FromSource("performance"))
                .WriteTo.RollingFile(formatter, Path.Combine(".\\logs\\", "log-perf-{Date}.log"))
                .WriteTo.Console(formatter))
            .CreateLogger();

            //Arrange
            var builder = new WebHostBuilder()
                .Configure((app) =>
                {
                    var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
                    loggerFactory.AddSerilog();
                    app.UseMiddleware<CorrelationIdMiddleware>("X-Correlation-Id");
                    app.UsePerformanceLog(options => options.Configure()
                        .WithFormat("request to {operation} took {duration}ms")
                        .WithLogLevel(LogLevel.Critical));
                    app.UseMiddleware<FakeMiddleware>(TimeSpan.FromMilliseconds(20));
                });

            var server = new TestServer(builder);

            //Act 
            var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "/delay/");
            requestMessage.Headers.TryAddWithoutValidation("X-Correlation-Id", "jonas");
            var responseMessage = await server.CreateClient().SendAsync(requestMessage);

        }



        [Fact, Trait("Category", "Usage")]
        public async void InvokeTest_TestWithSerilogAndNormal()
        {
            var formatter = new CustomJsonFormatter("testapp");
            Serilog.Log.Logger = new Serilog.LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Logger(lc => lc
                .Filter.ByExcluding(Matching.FromSource("performance"))
                .WriteTo.RollingFile(formatter, Path.Combine(".\\logs\\", "log-{Date}.log"))
                .WriteTo.Console(formatter))
            .WriteTo.Logger(lc => lc
                .Filter.ByIncludingOnly(Matching.FromSource("performance"))
                .WriteTo.RollingFile(formatter, Path.Combine(".\\logs\\", "log-perf-{Date}.log"))
                .WriteTo.Console(formatter))
            .CreateLogger();

            //Arrange
            var builder = new WebHostBuilder()
                .Configure((app) =>
                {
                    var loggerFactory = app.ApplicationServices.GetService<ILoggerFactory>();
                    loggerFactory.AddSerilog();
                    app.UseMiddleware<CorrelationIdMiddleware>("X-Correlation-Id");
                    app.UsePerformanceLog(options => options.Configure().WithFormat("request to {operation} took {duration}ms"));
                    app.UseMiddleware<FakeMiddleware>(TimeSpan.FromMilliseconds(20));
                });

            var server = new TestServer(builder);

            //Act 
            var requestMessage = new HttpRequestMessage(new HttpMethod("GET"), "/delay/");
            requestMessage.Headers.TryAddWithoutValidation("X-Correlation-Id", "jonas");
            var responseMessage = await server.CreateClient().SendAsync(requestMessage);

        }
    }
}
