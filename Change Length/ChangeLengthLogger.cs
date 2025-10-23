using System;
using System.Diagnostics;

namespace Quoc_MEP
{
    /// <summary>
    /// Logging utility cho Change Length command
    /// Logging utility for Change Length command
    /// </summary>
    public static class ChangeLengthLogger
    {
        private static readonly string LOG_PREFIX = "[ChangeLengthCmd]";

        /// <summary>
        /// Log an information message
        /// </summary>
        public static void Info(string message)
        {
            Debug.WriteLine($"{LOG_PREFIX} INFO: {message}");
            Trace.WriteLine($"{LOG_PREFIX} INFO: {message}");
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void Warning(string message)
        {
            Debug.WriteLine($"{LOG_PREFIX} WARNING: {message}");
            Trace.WriteLine($"{LOG_PREFIX} WARNING: {message}");
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public static void Error(string message)
        {
            Debug.WriteLine($"{LOG_PREFIX} ERROR: {message}");
            Trace.WriteLine($"{LOG_PREFIX} ERROR: {message}");
        }

        /// <summary>
        /// Log an error message with exception details
        /// </summary>
        public static void Error(string message, Exception ex)
        {
            Debug.WriteLine($"{LOG_PREFIX} ERROR: {message} - Exception: {ex.Message}");
            Trace.WriteLine($"{LOG_PREFIX} ERROR: {message} - Exception: {ex.Message}");
            if (ex.StackTrace != null)
            {
                Debug.WriteLine($"{LOG_PREFIX} Stack Trace: {ex.StackTrace}");
                Trace.WriteLine($"{LOG_PREFIX} Stack Trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Log the start of an operation
        /// </summary>
        public static void StartOperation(string operationName)
        {
            Info($"=== {operationName} START ===");
        }

        /// <summary>
        /// Log the end of an operation
        /// </summary>
        public static void EndOperation(string operationName)
        {
            Info($"=== {operationName} END ===");
        }
    }
}
