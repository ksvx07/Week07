using System;
using System.IO;
using UnityEngine;

public enum LogLevel
{
    Info,  // 일반 정보
    Warning, // 잠재적 문제
    Error  // 심각한 오류
}

public class GameLog : MonoBehaviour
{
    public static GameLog Instance { get; private set; }

    private string logFilePath;
    public string LogFilePath => logFilePath;

    #region /// 편의성을 위한 정적 메서드 ///

    // --- 다른 스크립트에서는 이 메서드들을 사용합니다! ---
    public static void Log(string message) => Instance?.Log(message, LogLevel.Info);
    public static void Warn(string message) => Instance?.Log(message, LogLevel.Warning);
    public static void Error(string message) => Instance?.Log(message, LogLevel.Error);

    #endregion

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        string exeDir = Path.GetDirectoryName(Application.dataPath);
        string logDir = Path.Combine(exeDir, "GameLog");
        Directory.CreateDirectory(logDir);

        string fileName = $"GameLog_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
        logFilePath = Path.Combine(logDir, fileName);

        this.Log("=== Game Session Started ===");
    }

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        string formattedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";

        switch (level)
        {
            case LogLevel.Info:
                Debug.Log(formattedMessage);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(formattedMessage);
                break;
            case LogLevel.Error:
                Debug.LogError(formattedMessage);
                break;
        }
        File.AppendAllText(logFilePath, formattedMessage + Environment.NewLine); // 파일에도 출력
    }

    [Button("로그 파일 열기")]
    /// <summary>
    /// 로그 파일이 저장된 폴더를 엽니다.
    /// </summary>
    private void OpenLogDirectory()
    {
        string exeDir = Path.GetDirectoryName(Application.dataPath);
        string logDir = Path.Combine(exeDir, "GameLog");

        if (Directory.Exists(logDir))
        {
            // 운영체제의 파일 탐색기로 해당 폴더를 엽니다.
            Application.OpenURL("file:///" + logDir);
        }
        else
        {
            Debug.LogWarning($"로그 폴더를 찾을 수 없습니다. 경로: {logDir}\n게임을 한 번 이상 실행하여 폴더를 생성해주세요.");
        }
    }


    // 모든 Debug 이벤트 등록용
    private void HandleUnityLog(string logString, string stackTrace, LogType type)
    {
        // 직접 출력한 로그는 이미 파일에 썼으므로, 중복 방지
        if (logString.StartsWith("["))
            return;

        string formatted = $"[{DateTime.Now:HH:mm:ss}] [{type}] {logString}";
        if (type == LogType.Error || type == LogType.Exception)
            formatted += $"\\n{stackTrace}";

        File.AppendAllText(logFilePath, formatted + Environment.NewLine);
    }

}
