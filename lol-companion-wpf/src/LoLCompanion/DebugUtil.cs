using System;
using System.IO;

namespace LoLCompanion
{
    public static class DebugUtil
    {
        private static string _logPath = "debug_log.txt";

        public static void LogDebug(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logLine = $"[{timestamp}] {message}";
                
                File.AppendAllText(_logPath, logLine + Environment.NewLine);
                System.Diagnostics.Debug.WriteLine(logLine);
            }
            catch { }
        }
    }
}
