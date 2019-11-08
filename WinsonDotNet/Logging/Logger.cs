using System;
using System.IO;
using System.Threading.Tasks;
using DSharpPlus;
using WinsonDotNet.Plugins;

namespace WinsonDotNet.Logging
{
    public class LogMessage
    {
        public LogLevel Severity { get; }
        public string Source { get; }
        public string Message { get; }
        public Exception Exception { get; }
        
        /// <summary>
        /// A specific type of log message capable of holding message and exception.
        /// </summary>
        /// <param name="severity"> The severity of the log.</param>
        /// <param name="source"> The source of the log</param>
        /// <param name="message"> The log message</param>
        /// <param name="exception"> An exception that need to be logged, default is null</param>
        public LogMessage(LogLevel severity, string source, string message, Exception exception = null)
        {
            Severity = severity;
            Source = source;
            Message = message;
            Exception = exception;
        }
    }
    
    public class Logger
    {
        private static LogLevel MaxConsoleLevel { get; } = LogLevel.Debug;

        private const string LogPath = @"logs";
        private const string LogFile = @"logs/Winson_Log";
        
        /// <summary>
        /// Static class to log data without passing by the service provider.
        /// </summary>
        /// <param name="logMessage"> The message to log.</param>
        public static async Task LogStatic(LogMessage logMessage)
        {
            if (logMessage.Severity <= MaxConsoleLevel) await LogToConsole(logMessage);
            await LogToFile(logMessage);
        }
        
        /// <summary>
        /// Log class to be used through the service provider.
        /// </summary>
        /// <param name="logMessage"> The message to log.</param>
        public async Task LogAsync(LogMessage logMessage)
        {
            if (logMessage.Severity <= MaxConsoleLevel) await LogToConsole(logMessage);
            await LogToFile(logMessage);
        }
        
        /// <summary>
        /// Log the specified message to the console.
        /// </summary>
        /// <param name="logMessage"> The message to log.</param>
        private static async Task LogToConsole(LogMessage logMessage)
        {
            switch (logMessage.Severity)
            {
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
                case LogLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case LogLevel.Warning:
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    break;
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevel.Critical:
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            await Console.Out.WriteAsync(
                $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] [{logMessage.Severity.ToString()}] [{logMessage.Source}] ");
            Console.ResetColor();
            if (logMessage.Exception != null)
                await Console.Out.WriteLineAsync($"{logMessage.Message}{Environment.NewLine}" +
                                                 $"The following exception occured: {logMessage.Exception.GetType()}" +
                                                 $"{Environment.NewLine}Message: {logMessage.Exception.Message}" +
                                                 $"{Environment.NewLine}Source: {logMessage.Exception.Source}" +
                                                 $"{Environment.NewLine}Help Link: {logMessage.Exception.HelpLink}" +
                                                 $"{Environment.NewLine}StackTrace: {logMessage.Exception.StackTrace}");
            else
                await Console.Out.WriteLineAsync($"{logMessage.Message}");
        }
        
        /// <summary>
        /// Log the specified message to a file.
        /// </summary>
        /// <param name="logMessage"> The message to log.</param>
        private static Task LogToFile(LogMessage logMessage)
        {
            if (!Directory.Exists(LogPath)) Directory.CreateDirectory(LogPath);

            var memory = GC.GetTotalMemory(false);

            if (logMessage.Exception != null)
                File.AppendAllText($"{LogFile}_{DateTime.Today:ddMMyyyy}.txt",
                    $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] " +
                    $"[RAM usage {Utility.GetRamUsage()}] " +
                    $"[{logMessage.Severity.ToString()}] [{logMessage.Source}] " +
                    $"{logMessage.Message}{Environment.NewLine}" +
                    $"The following exception occured: {logMessage.Exception.GetType()}" +
                    $"{Environment.NewLine}Message: {logMessage.Exception.Message}" +
                    $"{Environment.NewLine}Source: {logMessage.Exception.Source}" +
                    $"{Environment.NewLine}Help Link: {logMessage.Exception.HelpLink}" +
                    $"{Environment.NewLine}StackTrace: {logMessage.Exception.StackTrace}{Environment.NewLine}");
            else
                File.AppendAllText($"{LogFile}_{DateTime.Today:ddMMyyyy}.txt",
                $"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] " +
                $"[RAM usage {Utility.GetRamUsage()}] " +
                $"[{logMessage.Severity.ToString()}] [{logMessage.Source}] {logMessage.Message}{Environment.NewLine}");
            
            return Task.CompletedTask;
        }
    }
}