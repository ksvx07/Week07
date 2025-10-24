using UnityEngine;
 
public class LogTestScene : MonoBehaviour
{
    public string text;
    void Start()
    {
        Debug.Log("--- 이 로그는 기록되지 않습니다 ---");

        // 콘솔에 일반 정보 아이콘으로 표시됩니다.
        GameLog.Log($"디버그용");
        // 콘솔에 일반 정보 아이콘으로 표시됩니다.
        GameLog.Info($"게임 정보 확인용");
        // 콘솔에 노란색 경고 아이콘으로 표시됩니다.
        GameLog.Warn("콘솔창에 LogWarning로 표시됨");
        // 콘솔에 빨간색 에러 아이콘으로 표시됩니다.
        GameLog.Error("콘솔창에 LogError로 표시됨");

        GameLog.Log(text);
    }
}
