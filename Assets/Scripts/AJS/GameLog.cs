using System;
using System.IO;
using System.Linq;
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

    [Tooltip("체크하면 게임 로그를 텍스트 파일로 저장합니다.")]
    [SerializeField]
    private bool enableFileLogging = true;

    [InfoBox("로그 시스템 활성화", VisibleIf = "enableFileLogging")]
    [Header("로그 레벨 설정")]
    [ShowIf("enableFileLogging")]
    [Tooltip("기록할 최소 로그 레벨을 설정합니다.")]
    [SerializeField] private LogLevel minimumLogLevel = LogLevel.Debug;

    [Header("파일 이름 설정")]
    [ShowIf("enableFileLogging")]
    [Tooltip("파일 이름 변경 여부")]
    [SerializeField]
    private bool setFileName = false;

    [SerializeField]
    [InfoBox("원하는 파일 이름을 설정하세요", InfoBoxType.Warning, VisibleIf = "IsLogFileNameEmpty")]
    [InfoBox("사용할 수 없는 특수문자(<, >, :, \", /, \\, |, ?, *)가 포함되어 있습니다.", InfoBoxType.Error, VisibleIf = "IsFileNameInvalid")]
    [ShowIf("setFileName")]
    private string logFileName = null;

    private string logFilePath;
    public string LogFilePath => logFilePath;

    // --- 파일 I/O 성능 개선을 위해 StreamWriter 사용 ---
    private StreamWriter logWriter;


    #region /// 편의성을 위한 정적 메서드 ///

    // --- 다른 스크립트에서 사용! ---
    public static void Log(string message, UnityEngine.Object context = null) => Instance?.WriteLog(message, LogLevel.Debug, context);
    public static void Info(string message, UnityEngine.Object context = null) => Instance?.WriteLog(message, LogLevel.Info, context);
    public static void Warn(string message, UnityEngine.Object context = null) => Instance?.WriteLog(message, LogLevel.Warning, context);
    public static void Error(string message, UnityEngine.Object context = null) => Instance?.WriteLog(message, LogLevel.Error, context);

    #endregion

    private void OnValidate()
    {
        if(enableFileLogging == false)
        {
            setFileName = false;
        }
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!enableFileLogging) return;

        InitializeLogFile();
    }

    private void WriteLog(string message, LogLevel level = LogLevel.Debug, UnityEngine.Object context = null)
    {
        if (level < minimumLogLevel)
        {
            return;
        }

        // 결과: "02:05.500"
        // 파일에 기록될 메시지 형식: [시간] [로그레벨] 메시지
        string fileLogMessage = $"[{GetTimeStamp()}] [{level.ToString().ToUpper()}] {message}";
        // 유니티 콘솔에 출력될 메시지 형식: [시간] 메시지
        string consoleLogMessage = $"[{GetTimeStamp()}] {message}";

        UnityEngine.Object logContext = context ?? this;

        switch (level)
        {
            case LogLevel.Debug:
            case LogLevel.Info:
                Debug.Log(consoleLogMessage, logContext);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(consoleLogMessage, logContext);
                break;
            case LogLevel.Error:
                Debug.LogError(consoleLogMessage, logContext);
                break;
        }

        if (!enableFileLogging) return;
        if (enableFileLogging && logWriter != null)
        {
            logWriter.WriteLine(fileLogMessage);
        }
    }

    // --- 게임 종료 시 파일을 안전하게 닫도록 처리 ---
    private void OnDestroy()
    {
        if (logWriter != null)
        {
            Log("=== Game Session Ended ===");
            logWriter.Close();
            logWriter = null;
        }
    }

    private void InitializeLogFile()
    {
        try
        {
            string exeDir = Path.GetDirectoryName(Application.dataPath);
            string logDir = Path.Combine(exeDir, "GameLog");
            Directory.CreateDirectory(logDir);

            string fileName;
            if (setFileName && !string.IsNullOrEmpty(logFileName))
            {
                fileName = $"{logFileName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
            }
            else
            {
                if (setFileName && string.IsNullOrEmpty(logFileName))
                {
                    Debug.LogWarning("로그 파일 이름이 없어 기본 이름으로 생성됩니다.");
                }
                fileName = $"GameLog_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
            }

            logFilePath = Path.Combine(logDir, fileName);

            logWriter = new StreamWriter(logFilePath, true, System.Text.Encoding.UTF8);
            logWriter.AutoFlush = true; // 자동으로 버퍼를 비워 파일에 즉시 쓰도록 설정

            this.WriteLog("=== Game Session Started ===");
        }
        catch (Exception ex)
        {
            Debug.LogError($"로그 파일 초기화 실패: {ex.Message}");
            enableFileLogging = false; // 파일 쓰기 비활성화
        }
    }

    // 로그에 적힐 시간형식
    private string GetTimeStamp()
    {
        // Time.time 대신 Time.unscaledTime을 사용하고, 새로운 형식으로 변경
        TimeSpan timeSpan = TimeSpan.FromSeconds(Time.unscaledTime);
        // 분:초.십분의일초 형식 / 예시: - 총 유지 시간: 01:25.5
        return $"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}.{timeSpan.Milliseconds / 100}";
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

    [Button("현재 로그 텍스트 열기")]
    /// <summary>
    /// 현재 기록 중인 로그 파일을 직접 엽니다.
    /// </summary>
    private void OpenCurrentLogFile()
    {
        // 파일 로깅이 비활성화 상태이면 경고를 표시하고 함수를 종료합니다.
        if (!enableFileLogging)
        {
            Debug.LogWarning("파일 로깅이 비활성화되어 있어 로그 파일을 열 수 없습니다.");
            return;
        }

        // logFilePath가 유효하고, 해당 경로에 파일이 실제로 존재하는지 확인합니다.
        if (!string.IsNullOrEmpty(logFilePath) && File.Exists(logFilePath))
        {
            // 운영체제의 기본 프로그램으로 로그 파일을 엽니다.
            Application.OpenURL("file:///" + logFilePath);
        }
        else
        {
            // 파일이 아직 생성되지 않았거나 경로가 잘못된 경우 경고를 표시합니다.
            Debug.LogWarning($"로그 파일을 찾을 수 없습니다. 경로: {logFilePath}\n게임을 실행하여 로그 파일이 생성되었는지 확인해주세요.");
        }
    }

    [Button("북마크")]
    private void AddBookMarkLog()
    {
        if (!enableFileLogging) return;

        string fileLogMessage = $"[{GetTimeStamp()}] [BOOKMARK] {"====================================="}";
        if (enableFileLogging && logWriter != null)
        {
            logWriter.WriteLine(fileLogMessage);
        }
        Debug.LogWarning(fileLogMessage); ;
    }

    #region 인스펙터용 함수
    private bool IsLogFileNameEmpty()
    {
        return string.IsNullOrEmpty(logFileName);
    }

    /// <summary>
    /// 파일 이름이 유효하지 '않을' 경우 true를 반환합니다. (InfoBox 표시에 사용)
    /// </summary>
    private bool IsFileNameInvalid()
    {
        // 파일 이름이 비어있거나 null이면 유효하지 않은 상태가 아니므로 false 반환
        if (string.IsNullOrEmpty(logFileName)) return false;

        // 유효하지 않은 문자가 '하나라도' 포함되어 있으면 true 반환
        return logFileName.ToCharArray().Intersect(Path.GetInvalidFileNameChars()).Any();
    }
    //
    #endregion
}
