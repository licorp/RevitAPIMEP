using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Quoc_MEP
{
    public static class Logger
    {
        private static readonly string LogPrefix = "[PipeEndpointUpdater]";
        private static readonly string LogFilePath;
        private static readonly bool IsDebugMode;

        // Import Windows API for OutputDebugString
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern void OutputDebugString(string message);

        static Logger()
        {
            // Khởi tạo đường dẫn log file
            string tempPath = Path.GetTempPath();
            string fileName = $"PipeEndpointUpdater_{DateTime.Now:yyyyMMdd}.log";
            LogFilePath = Path.Combine(tempPath, fileName);

            // Kiểm tra debug mode
            IsDebugMode = IsDebugBuild();

            // Log khởi tạo
            LogInfo("Logger initialized");
            LogInfo($"Log file: {LogFilePath}");
            LogInfo($"Debug mode: {IsDebugMode}");
        }

        /// <summary>
        /// Ghi log thông tin
        /// </summary>
        /// <param name="message">Nội dung log</param>
        /// <param name="memberName">Tên method (tự động)</param>
        /// <param name="sourceFilePath">Đường dẫn file (tự động)</param>
        /// <param name="sourceLineNumber">Số dòng (tự động)</param>
        public static void LogInfo(string message, 
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            WriteLog("INFO", message, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Ghi log cảnh báo
        /// </summary>
        public static void LogWarning(string message,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            WriteLog("WARNING", message, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Ghi log lỗi
        /// </summary>
        public static void LogError(string message,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            WriteLog("ERROR", message, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Ghi log exception với stack trace
        /// </summary>
        public static void LogException(Exception ex, string additionalMessage = "",
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            string message = $"Exception: {ex.Message}";
            if (!string.IsNullOrEmpty(additionalMessage))
            {
                message = $"{additionalMessage} - {message}";
            }
            message += $"\nStack Trace: {ex.StackTrace}";
            
            WriteLog("EXCEPTION", message, memberName, sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Ghi log debug (chỉ trong debug mode)
        /// </summary>
        public static void LogDebug(string message,
            [System.Runtime.CompilerServices.CallerMemberName] string memberName = "",
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = "",
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = 0)
        {
            if (IsDebugMode)
            {
                WriteLog("DEBUG", message, memberName, sourceFilePath, sourceLineNumber);
            }
        }

        /// <summary>
        /// Ghi log với các parameter của method
        /// </summary>
        public static void LogMethodEntry(string methodName, params object[] parameters)
        {
            string paramStr = "";
            if (parameters != null && parameters.Length > 0)
            {
                paramStr = " with parameters: " + string.Join(", ", parameters);
            }
            LogDebug($"Entering method: {methodName}{paramStr}");
        }

        /// <summary>
        /// Ghi log khi thoát method
        /// </summary>
        public static void LogMethodExit(string methodName, object returnValue = null)
        {
            string returnStr = "";
            if (returnValue != null)
            {
                returnStr = $" returning: {returnValue}";
            }
            LogDebug($"Exiting method: {methodName}{returnStr}");
        }

        /// <summary>
        /// Core method để ghi log
        /// </summary>
        private static void WriteLog(string level, string message, string memberName, string sourceFilePath, int sourceLineNumber)
        {
            try
            {
                string fileName = Path.GetFileName(sourceFilePath);
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                string logEntry = $"{timestamp} [{level}] {LogPrefix} {fileName}:{sourceLineNumber} {memberName}() - {message}";

                // Ghi ra DebugView (OutputDebugString) - Method 1: System.Diagnostics
                Debug.WriteLine(logEntry);
                
                // Ghi ra DebugView (OutputDebugString) - Method 2: Direct API call
                OutputDebugString(logEntry + "\r\n");

                // Ghi ra file log
                WriteToFile(logEntry);

                // Ghi ra Console (nếu có)
                Console.WriteLine(logEntry);
                
                // Force immediate output
                Debug.Flush();
            }
            catch (Exception ex)
            {
                // Tránh infinite loop nếu logging bị lỗi
                OutputDebugString($"{LogPrefix} Logger error: {ex.Message}\r\n");
            }
        }

        /// <summary>
        /// Ghi log vào file
        /// </summary>
        private static void WriteToFile(string logEntry)
        {
            try
            {
                // Thread-safe file writing
                lock (LogFilePath)
                {
                    File.AppendAllText(LogFilePath, logEntry + Environment.NewLine);
                }
            }
            catch (Exception)
            {
                // Ignore file writing errors
            }
        }

        /// <summary>
        /// Kiểm tra có phải debug build không
        /// </summary>
        private static bool IsDebugBuild()
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                object[] attributes = assembly.GetCustomAttributes(typeof(DebuggableAttribute), false);
                
                if (attributes.Length > 0)
                {
                    DebuggableAttribute debugAttribute = attributes[0] as DebuggableAttribute;
                    return debugAttribute?.IsJITTrackingEnabled == true;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Xóa log files cũ (giữ lại 7 ngày)
        /// </summary>
        public static void CleanupOldLogs()
        {
            try
            {
                string tempPath = Path.GetTempPath();
                string[] logFiles = Directory.GetFiles(tempPath, "PipeEndpointUpdater_*.log");
                DateTime cutoffDate = DateTime.Now.AddDays(-7);

                foreach (string logFile in logFiles)
                {
                    FileInfo fileInfo = new FileInfo(logFile);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        File.Delete(logFile);
                        LogDebug($"Deleted old log file: {logFile}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error cleaning up old logs: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy đường dẫn log file hiện tại
        /// </summary>
        public static string GetLogFilePath()
        {
            return LogFilePath;
        }

        /// <summary>
        /// Log thông tin về Revit environment
        /// </summary>
        public static void LogRevitEnvironment()
        {
            try
            {
                LogInfo("=== Revit Environment Information ===");
                LogInfo($"Machine Name: {Environment.MachineName}");
                LogInfo($"User Name: {Environment.UserName}");
                LogInfo($"OS Version: {Environment.OSVersion}");
                LogInfo($"CLR Version: {Environment.Version}");
                LogInfo($"Working Directory: {Environment.CurrentDirectory}");
                LogInfo($"Process ID: {Process.GetCurrentProcess().Id}");
                LogInfo($"Assembly Location: {Assembly.GetExecutingAssembly().Location}");
                LogInfo("=========================================");
            }
            catch (Exception ex)
            {
                LogError($"Error logging Revit environment: {ex.Message}");
            }
        }
    }
}