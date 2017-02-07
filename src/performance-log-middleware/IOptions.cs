using Microsoft.Extensions.Logging;

namespace PerformanceLog
{
    public interface IOptions
    {
        IOptions WithLogLevel(LogLevel logLevel);
        IOptions WithFormat(string format);
    }
}