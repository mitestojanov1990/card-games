using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace CardGame.Utils
{
    public class GameLogger : MonoBehaviour
    {
        private static GameLogger instance;
        public static GameLogger Instance => instance;

        private string logFilePath;
        private StringBuilder logBuffer = new StringBuilder();
        private const int BUFFER_FLUSH_SIZE = 1000;
        private Queue<LogEntry> recentLogs = new Queue<LogEntry>();
        private const int MAX_RECENT_LOGS = 100;

        private struct LogEntry
        {
            public DateTime Timestamp;
            public string Message;
            public LogType Type;
            public string Context;
        }

        public enum LogType
        {
            Info,
            GameEvent,
            Warning,
            Error,
            Debug
        }

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeLogger();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeLogger()
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string logDirectory = Path.Combine(Application.persistentDataPath, "Logs");
            Directory.CreateDirectory(logDirectory);
            logFilePath = Path.Combine(logDirectory, $"game_log_{timestamp}.txt");

            // Subscribe to Unity's debug log
            Application.logMessageReceived += HandleUnityLog;

            // Write initial log entry
            Log($"=== Game Session Started at {timestamp} ===", LogType.Info);
            Log($"Game Version: {Application.version}", LogType.Info);
            Log($"Platform: {Application.platform}", LogType.Info);
        }

        public void Log(string message, LogType type = LogType.Info, string context = "")
        {
            LogEntry entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = message,
                Type = type,
                Context = context
            };

            string formattedLog = FormatLogEntry(entry);
            logBuffer.AppendLine(formattedLog);

            // Keep recent logs in memory
            recentLogs.Enqueue(entry);
            while (recentLogs.Count > MAX_RECENT_LOGS)
            {
                recentLogs.Dequeue();
            }

            // Flush buffer if it's getting large
            if (logBuffer.Length >= BUFFER_FLUSH_SIZE)
            {
                FlushBuffer();
            }
        }

        private string FormatLogEntry(LogEntry entry)
        {
            string contextInfo = string.IsNullOrEmpty(entry.Context) ? "" : $" [{entry.Context}]";
            return $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Type}]{contextInfo}: {entry.Message}";
        }

        private void FlushBuffer()
        {
            if (logBuffer.Length > 0)
            {
                try
                {
                    File.AppendAllText(logFilePath, logBuffer.ToString());
                    logBuffer.Clear();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to write to log file: {ex.Message}");
                }
            }
        }

        public void LogGameEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            StringBuilder eventLog = new StringBuilder();
            eventLog.Append(eventName);

            if (parameters != null && parameters.Count > 0)
            {
                eventLog.Append(" - Parameters: ");
                foreach (var param in parameters)
                {
                    eventLog.Append($"{param.Key}={param.Value}, ");
                }
                eventLog.Length -= 2; // Remove last comma and space
            }

            Log(eventLog.ToString(), LogType.GameEvent);
        }

        public string[] GetRecentLogs(int count = 10)
        {
            count = Mathf.Min(count, recentLogs.Count);
            string[] logs = new string[count];
            var tempQueue = new Queue<LogEntry>(recentLogs);

            while (tempQueue.Count > count)
            {
                tempQueue.Dequeue();
            }

            int i = 0;
            foreach (var entry in tempQueue)
            {
                logs[i++] = FormatLogEntry(entry);
            }

            return logs;
        }

        private void HandleUnityLog(string logString, string stackTrace, LogType type)
        {
            switch (type)
            {
                case UnityEngine.LogType.Error:
                case UnityEngine.LogType.Exception:
                    Log(logString + "\n" + stackTrace, LogType.Error);
                    break;
                case UnityEngine.LogType.Warning:
                    Log(logString, LogType.Warning);
                    break;
                default:
                    Log(logString, LogType.Debug);
                    break;
            }
        }

        private void OnDestroy()
        {
            FlushBuffer();
            Application.logMessageReceived -= HandleUnityLog;
        }

        private void OnApplicationQuit()
        {
            Log("=== Game Session Ended ===", LogType.Info);
            FlushBuffer();
        }
    }
} 