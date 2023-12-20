using System;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace RyanairFlightTrackBot
{

    public static class LoggerManager
    {
        internal static readonly object lockObject = new object();
        internal static Logger logger;

        internal static void InitialiseLogger()
        {
            lock (lockObject)
            {
                // Get the program's directory
                string programDirectory = AppDomain.CurrentDomain.BaseDirectory;

                // Create a directory path for logs with the current date and time
                string dateTimeNow = DateTime.Now.ToString("yyyyMMdd_HHmm");
                string logDirectory = Path.Combine(programDirectory, "logs");

                // Create the directory if it doesn't exist
                Directory.CreateDirectory(logDirectory);

                // Include timestamp in the log file name
                string logFileName = $"log_{dateTimeNow}.txt";

                // Configure NLog to write logs to the specified directory and log file
                LoggingConfiguration config = new LoggingConfiguration();
                FileTarget fileTarget = new FileTarget
                {
                    FileName = Path.Combine(logDirectory, logFileName),
                    Layout = "${longdate} ${level} ${message} ${exception}"
                };
                config.AddTarget("file", fileTarget);
                config.AddRuleForAllLevels(fileTarget);

                LogManager.Configuration = config;

                // Create the logger instance
                logger = LogManager.GetCurrentClassLogger();
            }
        }

        internal static Logger GetLogger()
        {
            // Provide a method to get the logger instance
            return logger ?? throw new InvalidOperationException("Logger not initialized. Call InitialiseLogger() first.");
        }
    }

}
