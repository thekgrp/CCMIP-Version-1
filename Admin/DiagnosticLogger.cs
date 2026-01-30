using System;
using VideoOS.Platform;

namespace CoreCommandMIP.Admin
{
    /// <summary>
    /// Diagnostic logger that writes to Milestone XProtect Management Server log
    /// using the Milestone ILog API (EnvironmentManager.Instance.Log).
    /// 
    /// All diagnostic messages are written to:
    /// C:\ProgramData\Milestone\XProtect Management Server\Logs\
    /// 
    /// To view logs:
    /// - Open Milestone Management Client
    /// - Go to Tools > Options > Logging
    /// - Or browse to the logs folder above
    /// </summary>
    internal static class DiagnosticLogger
    {
        private static readonly object _lockObject = new object();
        private const string SOURCE_NAME = "CoreCommandMIP";

        static DiagnosticLogger()
        {
            // Log initialization
            try
            {
                EnvironmentManager.Instance.Log(
                    false,
                    SOURCE_NAME,
                    "=".PadRight(80, '='));
                
                EnvironmentManager.Instance.Log(
                    false,
                    SOURCE_NAME,
                    "CoreCommandMIP Diagnostic Logging Started");
                
                EnvironmentManager.Instance.Log(
                    false,
                    SOURCE_NAME,
                    $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                
                EnvironmentManager.Instance.Log(
                    false,
                    SOURCE_NAME,
                    $"Machine: {Environment.MachineName}");
                
                EnvironmentManager.Instance.Log(
                    false,
                    SOURCE_NAME,
                    $"User: {Environment.UserName}");

                // Log Management Server connection info
                try
                {
                    var config = Configuration.Instance;
                    var serverFQID = config?.ServerFQID;

                    EnvironmentManager.Instance.Log(
                        false,
                        SOURCE_NAME,
                        $"Management Server: {serverFQID?.ServerId?.ServerType ?? "Unknown"}");

                    var serverId = serverFQID?.ServerId?.Id;
                    EnvironmentManager.Instance.Log(
                        false,
                        SOURCE_NAME,
                        $"Server ID: {(serverId.HasValue ? serverId.Value.ToString() : "Unknown")}");

                    EnvironmentManager.Instance.Log(
                        false,
                        SOURCE_NAME,
                        $"Connected: {(config != null ? "Yes" : "No")}");
                }
                catch (Exception ex)
                {
                    EnvironmentManager.Instance.Log(
                        true,
                        SOURCE_NAME,
                        $"Could not get Management Server info: {ex.Message}");
                }

                EnvironmentManager.Instance.Log(
                    false,
                    SOURCE_NAME,
                    "=".PadRight(80, '='));
            }
            catch
            {
                // Fallback to Debug output if logging fails
                System.Diagnostics.Debug.WriteLine("DiagnosticLogger: Failed to initialize Milestone logging");
            }
        }

        /// <summary>
        /// Write a log message to Milestone log
        /// </summary>
        public static void WriteLine(string message)
        {
            try
            {
                lock (_lockObject)
                {
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    var logMessage = $"[{timestamp}] {message}";

                    // Write to Milestone log
                    EnvironmentManager.Instance.Log(
                        false,  // isError = false
                        SOURCE_NAME,
                        logMessage);

                    // Also write to Debug output for Visual Studio debugging
                    System.Diagnostics.Debug.WriteLine(logMessage);
                }
            }
            catch (Exception ex)
            {
                // Fallback to Debug output only
                System.Diagnostics.Debug.WriteLine($"Log write failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Write an exception to Milestone log with full details
        /// </summary>
        public static void WriteException(string context, Exception ex)
        {
            try
            {
                lock (_lockObject)
                {
                    // Error header
                    EnvironmentManager.Instance.Log(
                        true,  // isError = true for exceptions
                        SOURCE_NAME,
                        $"EXCEPTION in {context}:");

                    // Exception details
                    EnvironmentManager.Instance.Log(
                        true,
                        SOURCE_NAME,
                        $"  Message: {ex.Message}");

                    EnvironmentManager.Instance.Log(
                        true,
                        SOURCE_NAME,
                        $"  Type: {ex.GetType().Name}");

                    if (ex.InnerException != null)
                    {
                        EnvironmentManager.Instance.Log(
                            true,
                            SOURCE_NAME,
                            $"  Inner Exception: {ex.InnerException.Message}");
                    }

                    // Stack trace (as regular log, not error, as it can be very long)
                    EnvironmentManager.Instance.Log(
                        false,
                        SOURCE_NAME,
                        $"  Stack Trace: {ex.StackTrace}");

                    // Also write to Debug for Visual Studio
                    System.Diagnostics.Debug.WriteLine($"ERROR in {context}: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"  Type: {ex.GetType().Name}");
                    System.Diagnostics.Debug.WriteLine($"  Stack: {ex.StackTrace}");
                }
            }
            catch (Exception logEx)
            {
                // Fallback to Debug output
                System.Diagnostics.Debug.WriteLine($"Exception logging failed: {logEx.Message}");
                System.Diagnostics.Debug.WriteLine($"Original exception: {ex.Message}");
            }
        }

        /// <summary>
        /// Write a section header to Milestone log
        /// </summary>
        public static void WriteSection(string title)
        {
            try
            {
                lock (_lockObject)
                {
                    EnvironmentManager.Instance.Log(
                        false,
                        SOURCE_NAME,
                        "");

                    EnvironmentManager.Instance.Log(
                        false,
                        SOURCE_NAME,
                        "=".PadRight(80, '='));

                    EnvironmentManager.Instance.Log(
                        false,
                        SOURCE_NAME,
                        $"  {title}");

                    EnvironmentManager.Instance.Log(
                        false,
                        SOURCE_NAME,
                        "=".PadRight(80, '='));

                    // Also write to Debug
                    System.Diagnostics.Debug.WriteLine($"\n=== {title} ===");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Section logging failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the path to Milestone log files
        /// </summary>
        public static string GetMilestoneLogPath()
        {
            return System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Milestone", "XProtect Management Server", "Logs");
        }
    }
}
