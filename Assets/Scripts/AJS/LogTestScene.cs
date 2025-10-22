using UnityEngine;
 
public class LogTestScene : MonoBehaviour
{
    void Start()
    {
        Debug.Log("--- 1. GameLogger 정적 메서드 테스트 시작 ---");

        // 콘솔에 일반 정보 아이콘으로 표시됩니다.
        GameLogger.Log("콘솔창에 일반적인 Log로 표시됨");

        // 콘솔에 노란색 경고 아이콘으로 표시됩니다.
        GameLogger.Warn("콘솔창에 LogWarning로 표시됨");

        // 콘솔에 빨간색 에러 아이콘으로 표시됩니다.
        GameLogger.Error("콘솔창에 LogError로 표시됨");

        Debug.Log("--- 1. GameLogger 정적 메서드 테스트 종료 ---\n");

    }
}
