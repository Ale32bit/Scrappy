using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrappy.Logger
{
    public class FileLogger : ILogger
    {
        private static ConcurrentQueue<string> awaitingLogs = new();
        private static bool processingLogs = false;

        public static Dictionary<LogLevel, string> StringLevels = new()
        {
            [LogLevel.Trace] = "TRCE",
            [LogLevel.Debug] = "DBUG",
            [LogLevel.Information] = "INFO",
            [LogLevel.Warning] = "WARN",
            [LogLevel.Error] = "FAIL",
            [LogLevel.Critical] = "CRIT",

        };

        public readonly string FilePath;

        public FileLogger(string filePath)
        {
            FilePath = filePath;
        }

        public IDisposable BeginScope<TState>(TState state) => default!;

        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel != LogLevel.None;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var now = DateTime.Now;
            var message = $"[{now.ToLongTimeString()} {StringLevels[logLevel]}] {formatter(state, exception)}{Environment.NewLine}";

            awaitingLogs.Enqueue(message);
            if (!processingLogs)
            {
                processingLogs = true;
                while (awaitingLogs.TryDequeue(out var log))
                {
                    try
                    {
                        File.AppendAllText(FilePath, log, Encoding.UTF8);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(message);
                        Console.Error.WriteLine(e);
                    }
                }
                processingLogs = false;
            }
        }
    }
}
