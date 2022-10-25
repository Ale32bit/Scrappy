using System.Collections.Concurrent;

namespace Scrappy.Logger
{
    internal class FileLoggerProvider : ILoggerProvider
    {
        public readonly string FilePath;
        private readonly ConcurrentDictionary<string, FileLogger> _loggers = new();
        public FileLoggerProvider(string filePath)
        {
            FilePath = filePath;
        }

        public ILogger CreateLogger(string categoryName)
        {
            var logger = new FileLogger(FilePath);
            _loggers.TryAdd(categoryName, logger);
            return logger;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
