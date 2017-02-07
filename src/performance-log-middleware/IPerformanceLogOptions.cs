namespace PerformanceLog
{
    public interface IPerformanceLogOptions
    {
        IOptions Configure();
        void Default();
    }
}