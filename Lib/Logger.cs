using System;
using System.Diagnostics;

namespace Quoc_MEP
{
    /// <summary>
    /// Centralized logging utility for Trans Data Para and other modules
    /// Outputs to DebugView and Visual Studio Output Window
    /// </summary>
    public static class Logger
    {
        private static readonly string LOG_PREFIX = "[TransDataPara]";
        private static bool _isInitialized = false;

        /// <summary>
        /// Initialize logger - adds DefaultTraceListener for DebugView support
        /// </summary>
        private static void Initialize()
        {
            if (_isInitialized) return;

            // Ensure DefaultTraceListener is present (for DebugView)
            if (Trace.Listeners.Count == 0 || !(Trace.Listeners[0] is DefaultTraceListener))
            {
                Trace.Listeners.Clear();
                Trace.Listeners.Add(new DefaultTraceListener());
            }

            // Enable auto-flush to ensure messages appear immediately
            Trace.AutoFlush = true;

            _isInitialized = true;
        }

        /// <summary>
        /// Log an information message
        /// </summary>
        public static void Info(string message)
        {
            Initialize();
            Trace.WriteLine($"{LOG_PREFIX} INFO: {message}");
            Debug.WriteLine($"{LOG_PREFIX} INFO: {message}"); // Also use Debug for DebugView
        }

        /// <summary>
        /// Log a warning message
        /// </summary>
        public static void Warning(string message)
        {
            Initialize();
            Trace.WriteLine($"{LOG_PREFIX} WARNING: {message}");
            Debug.WriteLine($"{LOG_PREFIX} WARNING: {message}");
        }

        /// <summary>
        /// Log an error message
        /// </summary>
        public static void Error(string message)
        {
            Initialize();
            Trace.WriteLine($"{LOG_PREFIX} ERROR: {message}");
            Debug.WriteLine($"{LOG_PREFIX} ERROR: {message}");
        }

        /// <summary>
        /// Log an error message with exception details
        /// </summary>
        public static void Error(string message, Exception ex)
        {
            Initialize();
            string errorMsg = $"{LOG_PREFIX} ERROR: {message} - Exception: {ex.Message}";
            Trace.WriteLine(errorMsg);
            Debug.WriteLine(errorMsg);
            
            if (ex.StackTrace != null)
            {
                string stackTrace = $"{LOG_PREFIX} Stack Trace: {ex.StackTrace}";
                Trace.WriteLine(stackTrace);
                Debug.WriteLine(stackTrace);
            }
        }

        /// <summary>
        /// Log a debug message (only in debug builds)
        /// </summary>
        [Conditional("DEBUG")]
        public static void DebugLog(string message)
        {
            Initialize();
            Trace.WriteLine($"{LOG_PREFIX} DEBUG: {message}");
            Debug.WriteLine($"{LOG_PREFIX} DEBUG: {message}");
        }

        /// <summary>
        /// Log the start of an operation
        /// </summary>
        public static void StartOperation(string operationName)
        {
            Initialize();
            string msg = $"=== {operationName} Started ===";
            Trace.WriteLine($"{LOG_PREFIX} {msg}");
            Debug.WriteLine($"{LOG_PREFIX} {msg}");
        }

        /// <summary>
        /// Log the end of an operation
        /// </summary>
        public static void EndOperation(string operationName)
        {
            Initialize();
            string msg = $"=== {operationName} Completed ===";
            Trace.WriteLine($"{LOG_PREFIX} {msg}");
            Debug.WriteLine($"{LOG_PREFIX} {msg}");
        }

        /// <summary>
        /// Log method entry with parameters (for debugging)
        /// </summary>
        [Conditional("DEBUG")]
        public static void MethodEntry(string methodName, params object[] parameters)
        {
            Initialize();
            string paramString = parameters != null && parameters.Length > 0 
                ? $" with parameters: {string.Join(", ", parameters)}" 
                : "";
            string msg = $"Entering method: {methodName}{paramString}";
            Trace.WriteLine($"{LOG_PREFIX} DEBUG: {msg}");
            Debug.WriteLine($"{LOG_PREFIX} DEBUG: {msg}");
        }

        /// <summary>
        /// Log method exit (for debugging)
        /// </summary>
        [Conditional("DEBUG")]
        public static void MethodExit(string methodName)
        {
            Initialize();
            string msg = $"Exiting method: {methodName}";
            Trace.WriteLine($"{LOG_PREFIX} DEBUG: {msg}");
            Debug.WriteLine($"{LOG_PREFIX} DEBUG: {msg}");
        }
    }
}
