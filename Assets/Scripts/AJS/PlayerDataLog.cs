using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class PlayerDataLog : MonoBehaviour
{
    // --- 데이터 추적을 위한 변수들 ---
    private PlayerShape currentShape;

    private int shapeChangeAmount; // 총 변신 횟수
    private int deadAmount;        // 총 죽음 횟수
    // 각 모양별 총 플레이 시간
    private Dictionary<PlayerShape, float> shapePlayTimes = new Dictionary<PlayerShape, float>();
    // 각 모양으로 몇 번 변신했는지 기록
    private Dictionary<PlayerShape, int> shapeChangeCounts = new Dictionary<PlayerShape, int>();
    // 각 모양의 최대 유지 시간
    private Dictionary<PlayerShape, float> maxShapeStayTimes = new Dictionary<PlayerShape, float>();

    // 현재 모양으로 변신한 시점의 시간 (연속 유지 시간 계산용)
    private float currentShapeStartTime;

    private void Update()
    {
        shapePlayTimes[currentShape] += Time.deltaTime;
    }

    public void PlayerLogStart(PlayerShape initialShape)
    {
        // 모든 PlayerShape Enum 값에 대해 Dictionary를 초기화합니다.
        foreach (PlayerShape shape in System.Enum.GetValues(typeof(PlayerShape)))
        {
            shapePlayTimes[shape] = 0f;
            shapeChangeCounts[shape] = 0;
            maxShapeStayTimes[shape] = 0f;
        }

        // 초기 모양 설정
        currentShape = initialShape;
        shapeChangeCounts[initialShape] = 1; // 시작 시 1회 변신(생성)으로 간주
        currentShapeStartTime = Time.time;   // 시작 시간 기록

        // 다른 카운터 초기화
        shapeChangeAmount = 1; // 시작도 변신 1회로 포함
        deadAmount = 0;
    }

    public void OnPlayerShapeChange(PlayerShape newShape)
    {
        if (newShape == currentShape) return; // 같은 모양으로 변경 요청 시 무시

        PlayerShape oldShape = currentShape;
        GameLog.Info($"모양 변경: {oldShape} -> {newShape} / {oldShape} 유지시간: {Time.time - currentShapeStartTime:F2}");

        // 이전 모양(oldShape)의 연속 유지 시간을 계산하고 최대값을 갱신
        UpdateMaxStayTime(oldShape);

        // 새로운 모양으로 데이터를 변경
        currentShape = newShape;
        currentShapeStartTime = Time.time; // 새 모양의 유지 시간 측정을 위해 현재 시간 기록
        shapeChangeCounts[newShape]++;     // 새 모양의 변신 횟수 증가
        shapeChangeAmount++;               // 전체 변신 횟수 증가

        // 3. 로그를 남깁니다.
    }

    /// <summary>
    /// 특정 모양의 최대 유지 시간을 계산하고 갱신합니다.
    /// </summary>
    private void UpdateMaxStayTime(PlayerShape shape)
    {
        float sessionDuration = Time.time - currentShapeStartTime;
        if (sessionDuration > maxShapeStayTimes[shape])
        {
            maxShapeStayTimes[shape] = sessionDuration;
        }
    }

    public void PlayerDeadLog()
    {
        deadAmount++;
        GameLog.Log($"플레이어 죽음 횟수: {deadAmount}번");
    }

    private void PlayerLogResult()
    {
        // shapePlayTimes를 기준으로 내림차순 정렬
        var sortedShapes = shapePlayTimes.Keys.OrderByDescending(shape => shapePlayTimes[shape]);

        // StringBuilder를 사용해 여러 줄의 문자열을 효율적으로 만듭니다.
        StringBuilder report = new StringBuilder();
        report.AppendLine(); // 보기 좋게 한 줄 띄우기
        report.AppendLine("--------- 최종 플레이어 데이터 ---------");
        report.AppendLine($"총 변신 횟수: {shapeChangeAmount}번");
        report.AppendLine($"총 죽음 횟수: {deadAmount}번");
        report.AppendLine("------------------------------------");
        report.AppendLine("[모양별 상세 기록 (플레이 시간 순)]");

        int rank = 1;
        foreach (var shape in sortedShapes)
        {
            string shapeName = shape.ToString();
            float totalTime = shapePlayTimes[shape];
            int changeCount = shapeChangeCounts[shape];
            float maxStayTime = maxShapeStayTimes[shape];

            report.AppendLine($"{rank}. {shapeName}");
            report.AppendLine($"   - 총 유지 시간: {totalTime:F2}초");
            report.AppendLine($"   - 변신 횟수: {changeCount}회");
            report.AppendLine($"   - 최대 연속 유지 시간: {maxStayTime:F2}초");
            rank++;
        }
        report.AppendLine("------------------------------------");

        // 최종적으로 만들어진 문자열을 한 번에 로그로 출력합니다.
        GameLog.Info(report.ToString());
    }

    // Hack : 게임 종료 시 확인을 위한 임시함수
    private void OnApplicationQuit()
    {
        // 마지막 모양의 연속 유지 시간도 계산에 포함시켜야 합니다.
        UpdateMaxStayTime(currentShape);
        PlayerLogResult();
    }
}
