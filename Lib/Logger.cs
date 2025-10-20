using System;
using System.Diagnostics;

namespace Quoc_MEP
{
    /// <summary>
    /// Centralized logging utility for the Rotate Elements addon
    /// </summary>
    public static class Logger
    {
        private static readonly string LOG_PREFIX = "[LicorpRotateAddin]";

        /// <summary>
        /// Log an information message
        /// </summary>
        public static void Info(string message)
        {
            Trace.WriteLine($"{LOG_PREFIX} INFO: {message}");
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void Warning(string message)
        {
            Trace.WriteLine($"{LOG_PREFIX} WARNING: {message}");
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public static void Error(string message)
        {
            Trace.WriteLine($"{LOG_PREFIX} ERROR: {message}");
        }

        /// <summary>
        /// Log an error message with exception details
        /// </summary>
        public static void Error(string message, Exception ex)
        {
            Trace.WriteLine($"{LOG_PREFIX} ERROR: {message} - Exception: {ex.Message}");
            if (ex.StackTrace != null)
            {
                Trace.WriteLine($"{LOG_PREFIX} Stack Trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Log a debug message (only in debug builds)
        /// </summary>
        [Conditional("DEBUG")]
        public static void Debug(string message)
        {
            Trace.WriteLine($"{LOG_PREFIX} DEBUG: {message}");
        }

        /// <summary>
        /// Log the start of an operation
        /// </summary>
        public static void StartOperation(string operationName)
        {
            Info($"=== {operationName} Started ===");
        }

        /// <summary>
        /// Log the end of an operation
        /// </summary>
        public static void EndOperation(string operationName)
        {
            Info($"=== {operationName} Completed ===");
        }

        /// <summary>
        /// Log method entry with parameters (for debugging)
        /// </summary>
        [Conditional("DEBUG")]
        public static void MethodEntry(string methodName, params object[] parameters)
        {
            string paramString = parameters != null && parameters.Length > 0 
                ? $" with parameters: {string.Join(", ", parameters)}" 
                : "";
            Debug($"Entering method: {methodName}{paramString}");
        }

        /// <summary>
        /// Log method exit (for debugging)
        /// </summary>
        [Conditional("DEBUG")]
        public static void MethodExit(string methodName)
        {
            Debug($"Exiting method: {methodName}");
        }
    }
}
