using System;
using System.IO;
using UnityEngine;

namespace DinoAlkkagi.Utility
{
    /// <summary>
    /// 모든 Debug.Log를 파일로 저장한다.
    /// 이전 프로젝트 방식: Application.persistentDataPath/logs/ 에 저장.
    /// </summary>
    public static class LogFileWriter
    {
        private static string logFilePath;
        private static bool initialized = false;
        private static StreamWriter writer;
        private static readonly object lockObj = new object();

        /// <summary>
        /// 로그 파일 기록을 시작한다.
        /// 자동 시작은 RuntimeInitializeOnLoadMethod로 처리.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (initialized) return;
            initialized = true;

            try
            {
                string logDir = Path.Combine(Application.persistentDataPath, "logs");
                if (!Directory.Exists(logDir))
                    Directory.CreateDirectory(logDir);

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                logFilePath = Path.Combine(logDir, $"DinoAlkkagi_{timestamp}.log");

                writer = new StreamWriter(logFilePath, append: true, encoding: System.Text.Encoding.UTF8);
                writer.AutoFlush = true;

                Application.logMessageReceived += OnLogMessage;

                Debug.Log($"[LogFileWriter] Log file: {logFilePath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[LogFileWriter] Failed to initialize: {ex.Message}");
            }
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            if (writer == null) return;

            lock (lockObj)
            {
                try
                {
                    string prefix = type switch
                    {
                        LogType.Error => "[ERROR]",
                        LogType.Assert => "[ASSERT]",
                        LogType.Warning => "[WARN]",
                        LogType.Log => "[INFO]",
                        LogType.Exception => "[EXCEPTION]",
                        _ => "[INFO]"
                    };

                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    writer.WriteLine($"{timestamp} {prefix} {message}");

                    if (type == LogType.Error || type == LogType.Exception)
                    {
                        writer.WriteLine($"{timestamp} {prefix} STACK: {stackTrace}");
                    }
                }
                catch
                {
                    // 로그 파일 쓰기 실패는 무시
                }
            }
        }

        /// <summary>
        /// 현재 로그 파일 경로를 반환한다.
        /// </summary>
        public static string GetLogFilePath()
        {
            return logFilePath ?? "Not initialized";
        }
    }
}
