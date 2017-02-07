using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using PerformanceLog;

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
}