using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

// 스테이지 또는 체크포인트별 데이터를 담는 클래스
public class LogStats
{
    public int deadAmount = 0;
    public int abilityUseAmount = 0;
    public int shapeChangeAmount = 0;

    public int modeSwitchAmount = 0;
    public int quickSwitchAmount = 0;

    public float totalPlayTime = 0f;

    public Dictionary<PlayerShape, ShapeLogDetail> shapeDetails = new Dictionary<PlayerShape, ShapeLogDetail>();

    public LogStats()
    {
        foreach (PlayerShape shape in Enum.GetValues(typeof(PlayerShape)))
        {
            shapeDetails[shape] = new ShapeLogDetail();
        }
    }
}

// 스테이지 하나에 대한 모든 데이터 (스테이지 전체 기록 + 체크포인트별 기록)
public class StageLogData
{
    public string stageName;
    public LogStats stageTotalStats = new LogStats(); // 스테이지 전체에 대한 누적 데이터
    public Dictionary<string, LogStats> checkpointStats = new Dictionary<string, LogStats>(); // 이 스테이지의 체크포인트별 데이터

    public StageLogData(string name)
    {
        this.stageName = name;
    }
}

// 각 모양에 대한 상세 데이터를 담는 클래스
public class ShapeLogDetail
{
    public int changeCount = 0;
    public float playTime = 0f;
    public int abilityUseCount = 0;
    public int deadCount = 0;
}


public class PlayerDataLog : MonoBehaviour
{
    // --- 전역 데이터 추적 변수들 ---
    private PlayerShape currentShape;
    private int shapeChangeAmount;
    private int deadAmount;
    private int modeSwitchAmount;
    private int quickSwitchAmount;
    private Dictionary<PlayerShape, float> shapePlayTimes = new Dictionary<PlayerShape, float>();
    private Dictionary<PlayerShape, int> shapeChangeCounts = new Dictionary<PlayerShape, int>();
    private Dictionary<PlayerShape, float> maxShapeStayTimes = new Dictionary<PlayerShape, float>();
    private Dictionary<PlayerShape, int> shapeAbilityCounts = new Dictionary<PlayerShape, int>();
    private float currentShapeStartTime;

    // --- 스테이지/체크포인트 데이터 ---
    private Dictionary<string, StageLogData> allStageLogs = new Dictionary<string, StageLogData>();

    // --- 현재 추적중인 스코프 ---
    private StageLogData currentStageLog;
    private LogStats currentCheckpointLog;


    private void Update()
    {
        // currentShape가 초기화된 후에만 실행
        if (shapePlayTimes.ContainsKey(currentShape))
        {
            float deltaTime = Time.unscaledDeltaTime;
            shapePlayTimes[currentShape] += deltaTime;

            // 현재 스테이지/체크포인트의 모양별 유지 시간을 누적
            if (currentStageLog != null)
            {
                currentStageLog.stageTotalStats.shapeDetails[currentShape].playTime += deltaTime;
                currentStageLog.stageTotalStats.totalPlayTime += deltaTime;
            }
            if (currentCheckpointLog != null)
            {
                currentCheckpointLog.shapeDetails[currentShape].playTime += deltaTime;
                currentCheckpointLog.totalPlayTime += deltaTime;
            }
        }
    }

    public void PlayerLogStart(PlayerShape initialShape)
    {
        foreach (PlayerShape shape in Enum.GetValues(typeof(PlayerShape)))
        {
            shapePlayTimes[shape] = 0f;
            shapeChangeCounts[shape] = 0;
            maxShapeStayTimes[shape] = 0f;
            shapeAbilityCounts[shape] = 0;
        }

        currentShape = initialShape;
        shapeChangeCounts[initialShape] = 1;
        currentShapeStartTime = Time.time;

        shapeChangeAmount = 0;
        deadAmount = 0;
        modeSwitchAmount = 0;
        quickSwitchAmount = 0;
    }

    // =================================================================
    //            새로운 외부 호출 함수 (스테이지/체크포인트 관리)
    // =================================================================

    /// <summary>
    /// 새로운 스테이지에 진입했을 때 호출합니다.
    /// </summary>
    /// <param name="stageId">스테이지를 식별할 고유한 이름 (예: "Stage 1", "Forest Area")</param>
    public void OnEnterStage(string stageId)
    {
        if (!allStageLogs.ContainsKey(stageId))
        {
            allStageLogs[stageId] = new StageLogData(stageId);
        }
        currentStageLog = allStageLogs[stageId];
        currentCheckpointLog = null; // 스테이지가 바뀌면 현재 체크포인트는 리셋
        GameLog.Info($"스테이지 [{stageId}] 진입");
    }

    /// <summary>
    /// 체크포인트에 도달했을 때 호출합니다.
    /// </summary>
    /// <param name="checkpointId">체크포인트를 식별할 고유한 이름 (예: "CP-1", "Mid-Boss Entrance")</param>
    public void OnReachCheckpoint(string checkpointId)
    {
        if (currentStageLog == null)
        {
            GameLog.Warn("현재 스테이지가 설정되지 않았는데 체크포인트 도달을 시도했습니다.");
            return;
        }

        if (!currentStageLog.checkpointStats.ContainsKey(checkpointId))
        {
            currentStageLog.checkpointStats[checkpointId] = new LogStats();
        }
        currentCheckpointLog = currentStageLog.checkpointStats[checkpointId];
        GameLog.Info($"체크포인트 [{checkpointId}] 도달");
    }

    // =================================================================
    //                    기존 함수 수정 (데이터 기록 로직 추가)
    // =================================================================

    private void OnPlayerShapeChange(PlayerShape newShape)
    {
        if (newShape == currentShape) return;

        PlayerShape oldShape = currentShape;

        UpdateMaxStayTime(oldShape);

        currentShape = newShape;
        currentShapeStartTime = Time.time;

        // --- 전역 데이터 기록 ---
        shapeChangeCounts[newShape]++;
        shapeChangeAmount++;

        // --- 스테이지/체크포인트 데이터 기록 ---
        if (currentStageLog != null)
        {
            currentStageLog.stageTotalStats.shapeChangeAmount++;
            currentStageLog.stageTotalStats.shapeDetails[newShape].changeCount++;
        }
        if (currentCheckpointLog != null)
        {
            currentCheckpointLog.shapeChangeAmount++;
            currentCheckpointLog.shapeDetails[newShape].changeCount++;
        }
    }

    public void PlayerDeadLog()
    {
        // --- 전역 데이터 기록 ---
        deadAmount++;

        // --- 스테이지/체크포인트 데이터 기록 ---
        if (currentStageLog != null)
        {
            currentStageLog.stageTotalStats.deadAmount++;
            currentStageLog.stageTotalStats.shapeDetails[currentShape].deadCount++; // 추가
        }
        if (currentCheckpointLog != null)
        {
            currentCheckpointLog.deadAmount++;
            currentCheckpointLog.shapeDetails[currentShape].deadCount++; // 추가
        }
    }

    public void OnPlayerUseAbility()
    {
        // --- 전역 데이터 기록 ---
        shapeAbilityCounts[currentShape]++;

        // --- 스테이지/체크포인트 데이터 기록 ---
        if (currentStageLog != null)
        {
            currentStageLog.stageTotalStats.abilityUseAmount++;
            currentStageLog.stageTotalStats.shapeDetails[currentShape].abilityUseCount++; // 추가
        }
        if (currentCheckpointLog != null)
        {
            currentCheckpointLog.abilityUseAmount++;
            currentCheckpointLog.shapeDetails[currentShape].abilityUseCount++; // 추가
        }
    }

    public void OnPlayerQuickSwitch(PlayerShape newShape)
    {
        if (newShape == currentShape) return;
        quickSwitchAmount++;

        if (currentStageLog != null) currentStageLog.stageTotalStats.quickSwitchAmount++;
        if (currentCheckpointLog != null) currentCheckpointLog.quickSwitchAmount++;

        OnPlayerShapeChange(newShape);
    }

    public void OnPlayerModeSwitch(PlayerShape newShape)
    {
        if (newShape == currentShape) return;
        modeSwitchAmount++;

        if (currentStageLog != null) currentStageLog.stageTotalStats.modeSwitchAmount++;
        if (currentCheckpointLog != null) currentCheckpointLog.modeSwitchAmount++;

        OnPlayerShapeChange(newShape);
    }

    private void UpdateMaxStayTime(PlayerShape shape)
    {
        float sessionDuration = Time.time - currentShapeStartTime;
        if (sessionDuration > maxShapeStayTimes[shape])
        {
            maxShapeStayTimes[shape] = sessionDuration;
        }
    }

    // =================================================================
    //                     결과 출력 함수 확장
    // =================================================================
    private void PlayerLogResult()
    {
        // StringBuilder를 사용해 여러 줄의 문자열을 효율적으로 만듭니다.
        StringBuilder report = new StringBuilder();
        report.AppendLine();
        report.AppendLine("#########################################");
        report.AppendLine("      >>>>> 최종 플레이어 데이터 <<<<<");
        report.AppendLine("#########################################");
        report.AppendLine();
        report.AppendLine("--------- [전체 플레이 요약] ---------");
        report.AppendLine($"총 변신 횟수: {shapeChangeAmount}번");
        report.AppendLine($"총 모드 변신 횟수: {modeSwitchAmount}번");
        report.AppendLine($"총 단축키 변신 횟수: {quickSwitchAmount}번");
        report.AppendLine($"총 죽음 횟수: {deadAmount}번");
        report.AppendLine("------------------------------------");
        report.AppendLine();
        report.AppendLine("--------- [모양별 상세 기록 (플레이 시간 순)] ---------");

        var sortedShapesByTime = shapePlayTimes.Keys.OrderByDescending(shape => shapePlayTimes[shape]);

        int rank = 1;
        foreach (var shape in sortedShapesByTime)
        {
            string shapeName = shape.ToString();
            float totalTime = shapePlayTimes[shape];
            int changeCount = shapeChangeCounts[shape];
            float maxStayTime = maxShapeStayTimes[shape];
            int abiltyCount = shapeAbilityCounts[shape];

            TimeSpan totalTimeSpan = TimeSpan.FromSeconds(totalTime);
            TimeSpan maxStayTimeSpan = TimeSpan.FromSeconds(maxStayTime);

            string formattedTotalTime = $"{totalTimeSpan.Minutes:D2}분: {totalTimeSpan.Seconds:D2}.{totalTimeSpan.Milliseconds / 100}";
            string formattedMaxStayTime = $"{maxStayTimeSpan.Minutes:D2}분: {maxStayTimeSpan.Seconds:D2}.{maxStayTimeSpan.Milliseconds / 100}";

            report.AppendLine($"{rank}. {shapeName}");
            report.AppendLine($"    - 총 유지 시간: {formattedTotalTime}초");
            report.AppendLine($"    - 변신 횟수: {changeCount}회");
            report.AppendLine($"    - 최대 연속 유지 시간: {formattedMaxStayTime}초");
            report.AppendLine($"    - 능력 사용 횟수: {abiltyCount}회");
            rank++;
        }
        report.AppendLine("-------------------------------------------------");
        report.AppendLine();

        // --- 스테이지별 상세 기록 출력 ---
        if (allStageLogs.Count > 0)
        {
            report.AppendLine("#########################################");
            report.AppendLine("      >>>>> 스테이지별 상세 데이터 <<<<<");
            report.AppendLine("#########################################");

            foreach (var stagePair in allStageLogs)
            {
                StageLogData stageData = stagePair.Value;
                report.AppendLine();
                report.AppendLine($"---------- [🧊 스테이지: {stageData.stageName}] ----------");

                // 스테이지 전체 기록 출력
                report.Append(GenerateScopeReport("\n📝 스테이지 요약", stageData.stageTotalStats));

                // 해당 스테이지의 체크포인트별 기록 출력
                if (stageData.checkpointStats.Count > 0)
                {
                    report.AppendLine();
                    report.AppendLine("    --- [체크포인트별 기록] ---");
                    foreach (var checkpointPair in stageData.checkpointStats)
                    {
                        report.Append(GenerateScopeReport("\n✅ 체크포인트 " + checkpointPair.Key, checkpointPair.Value, "      "));
                    }
                }
            }
        }

        // 최종적으로 만들어진 문자열을 한 번에 로그로 출력합니다.
        GameLog.Info(report.ToString());
    }

    /// <summary>
    /// 체크포인트별 기록 문자열을 생성하는 헬퍼 함수
    /// </summary>
    private string GenerateScopeReport(string title, LogStats stats, string indentation = "  ")
    {
        StringBuilder sb = new StringBuilder();

        TimeSpan totalTimeSpan = TimeSpan.FromSeconds(stats.totalPlayTime);
        string formattedTotalTime = $"[{totalTimeSpan.Minutes:D2}:{totalTimeSpan.Seconds:D2}]";

        string shapeChangeDetail = "(";
        if (stats.shapeChangeAmount > 0)
        {
            if (stats.modeSwitchAmount > 0)
            {
                shapeChangeDetail += "모드 : " + stats.modeSwitchAmount;
            }
            if (stats.quickSwitchAmount > 0)
            {
                if (stats.modeSwitchAmount > 0)
                {
                    shapeChangeDetail += " / ";
                }
                shapeChangeDetail += "단축키 : " + stats.quickSwitchAmount + ")";
            }
        }
        else
        {
            shapeChangeDetail = string.Empty;
        }

        sb.AppendLine($"{indentation}{title}: 🕒 시간 {formattedTotalTime} | 💀 죽음 {stats.deadAmount} | ✨ 능력 {stats.abilityUseAmount} | 🔄 변신 {stats.shapeChangeAmount} {shapeChangeDetail}");
        // 변신 횟수가 0보다 클 때만 세부 정보 표시

        // 변신한 모양이 있을 경우에만 상세 내역 출력 (shapeDetails.changeCount > 0)
        var changedShapesDetails = stats.shapeDetails
            .Where(kv => kv.Value.changeCount > 0 || kv.Value.abilityUseCount > 0 || kv.Value.deadCount > 0 || kv.Value.playTime > 0.1f)
            .OrderByDescending(kv => kv.Value.playTime);

        if (changedShapesDetails.Any())
        {
            sb.AppendLine($"{indentation}    ⏳ 모양별 상세 내역 (유지 시간 순)");
            foreach (var pair in changedShapesDetails)
            {
                PlayerShape shape = pair.Key;
                ShapeLogDetail detail = pair.Value;

                TimeSpan timeSpan = TimeSpan.FromSeconds(detail.playTime);
                string formattedTime = $"[{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}]";

                // 각 항목이 0이 아닐 경우에만 문자열에 추가하여 간결하게 표시
                List<string> details = new List<string>();
                details.Add($"💀 죽음 {detail.deadCount,2} ");
                details.Add($"✨ 능력 {detail.abilityUseCount,2} ");
                details.Add($"🔄 변신 {detail.changeCount,2} ");

                string detailString = details.Any() ? $" {string.Join("| ", details)}" : "";

                sb.AppendLine($"{indentation}         ➡️ {GetShapeIcon(shape).PadRight(2)}{formattedTime.PadRight(10)}{detailString.PadRight(10)}");
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// PlayerShape에 해당하는 아이콘 문자열을 반환합니다.
    /// 실제 게임의 PlayerShape enum에 맞게 아이콘을 추가/변경하세요.
    /// </summary>
    private string GetShapeIcon(PlayerShape shape)
    {

        switch (shape)
        {
            case PlayerShape.Circle:
                return "⏺️ "; // 원
            case PlayerShape.Star:
                return "⭐"; // 별
            case PlayerShape.Square:
                return "⏹️ "; // 네모
            case PlayerShape.Triangle:
                return "🔼 "; // 세모
            default:
                return "❓"; // 알 수 없는 모양
        }
    }

    private void OnApplicationQuit()
    {
        // 마지막 모양의 연속 유지 시간도 계산에 포함시켜야 합니다.
        if (shapePlayTimes.ContainsKey(currentShape))
        {
            UpdateMaxStayTime(currentShape);
        }
        PlayerLogResult();
    }
}