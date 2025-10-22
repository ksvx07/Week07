using System;
using System.IO;
using UnityEngine;

public enum LogLevel
{
    Debug, // 디버깅용
    Info,  // 게임 정보
    Warning, // 잠재적 문제
    Error  // 심각한 오류
}
public class GameLog : MonoBehaviour
{
    public static GameLog Instance { get; private set; }

    private string logFilePath;
    public string LogFilePath => logFilePath;

    public bool setFileName = false;

    [SerializeField]
    [InfoBox("원하는 파일 이름을 설정하세요", InfoBoxType.Warning, VisibleIf = "IsLogFileNameEmpty")]
    [ShowIf("setFileName")]
    private string logFileName = null;


    #region /// 편의성을 위한 정적 메서드 ///

    // --- 다른 스크립트에서 사용! ---
    public static void Log(string message) => Instance?.Log(message, LogLevel.Debug);
    public static void Info(string message) => Instance?.Log(message, LogLevel.Info);
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

        // 파일 이름 결정 로직
        string fileName;

        if (setFileName)
        {
            if (!string.IsNullOrEmpty(logFileName))
            {
                fileName = $"{logFileName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
                logFileName = string.Empty;
            }
            else
            {
                Debug.LogWarning("로그파일 이름이 없습니다");
                fileName = $"Null_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
            }
        }
        else
        {
            fileName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
        }

        logFilePath = Path.Combine(logDir, fileName);

        this.Log("=== Game Session Started ===");
    }

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        // 파일에 기록될 메시지 형식: [시간] [로그레벨] 메시지
        string fileLogMessage = $"[{DateTime.Now:HH:mm:ss}] [{level.ToString().ToUpper()}] {message}";
        // 유니티 콘솔에 출력될 메시지 형식: [시간] 메시지
        string consoleLogMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";

        switch (level)
        {
            case LogLevel.Debug:
            case LogLevel.Info:
                Debug.Log(consoleLogMessage);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(consoleLogMessage);
                break;
            case LogLevel.Error:
                Debug.LogError(consoleLogMessage);
                break;
        }
        File.AppendAllText(logFilePath, fileLogMessage + Environment.NewLine); // 파일에도 출력
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

    #region 인스펙터용 함수
    private bool IsLogFileNameEmpty()
    {
        return string.IsNullOrEmpty(logFileName);
    }
    #endregion
}
