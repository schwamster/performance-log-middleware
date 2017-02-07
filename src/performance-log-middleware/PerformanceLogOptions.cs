using Microsoft.Extensions.Logging;

namespace PerformanceLog
{
    public class PerformanceLogOptions : IPerformanceLogOptions, IOptions
    {
        public LogLevel LogLevel { get; set; }

        public string Format { get; set; }

        public PerformanceLogOptions()
        {
            Default();
        }

        public void Default()
        {
            LogLevel = LogLevel.Information;
            Format = "request to {Operation} took {Duration}ms";
        }

        public IOptions WithFormat(string format)
        {
            this.Format = format;
            return this;
        }

        public IOptions Configure()
        {
            return this;
        }

        public IOptions WithLogLevel(LogLevel logLevel)
        {
            this.LogLevel = logLevel;
            return this;
        }

    }
}