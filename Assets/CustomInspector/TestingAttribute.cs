using UnityEngine;

public class TestingAttribute : MonoBehaviour
{
    // =================================================================
    // [ShowIf] / [HideIf] 어트리뷰트 테스트
    // =================================================================
    [Header("ShowIf / HideIf")]
    public bool showConditionalFields;

    [ShowIf("showConditionalFields")]
    public string showIfText = "이 필드는 위 체크박스가 켜지면 보입니다.";

    [HideIf("showConditionalFields")]
    public int hideIfValue = 100; // 이 필드는 위 체크박스가 꺼지면 보입니다.

    public enum IconType { Info, Error, Warning }
    public IconType iconType;

    [InfoBox("아이콘 타입이 Info 일 때만 보입니다.", InfoBoxType.Info)]
    [ShowIf("IsInfo")] // 메서드를 조건으로 사용
    public int HP = 15;


    [InfoBox("아이콘 타입이 Warning 일 때만 보입니다.", InfoBoxType.Warning)]
    [ShowIf("IsWarning")] // 메서드를 조건으로 사용
    public bool check;
    [InfoBox("아이콘 타입이 Error 일 때만 보입니다.", InfoBoxType.Error)]
    [ShowIf("IsError")] // 메서드를 조건으로 사용
    public int MP = 15;


    // ShowIf의 조건으로 사용될 메서드들 (private이어도 상관없습니다)
    private bool IsInfo() => iconType == IconType.Info;
    private bool IsError() => iconType == IconType.Error;
    private bool IsWarning() => iconType == IconType.Warning;

    [Header("HP_Setting")]
    // --- 동적 메시지 테스트 ---
    [Range(0, 100)]
    public int health = 100;
    [InfoBox("$GetHealthStatus", InfoBoxType.Info)]

    // 다른 필드의 값을 메시지로 사용
    [InfoBox("$dynamicMessageFromField", InfoBoxType.Warning)]
    //[SerializeField] private string dynamicMessageFromField = "이 텍스트를 수정하면 위의 InfoBox도 바뀝니다.";

    private string GetHealthStatus()
    {
        if (health > 70) return $"[메서드] 상태 좋음! 현재 체력: {health}";
        if (health > 30) return $"[메서드] 부상! 현재 체력: {health}";
        return $"[메서드] 위험! 현재 체력: {health}";
    }

    // --- VisibleIf 테스트 ---
    [Space(10)]
    public bool showAlertBox;

    [InfoBox("이 메시지는 위 체크박스가 켜져 있을 때만 보입니다.", InfoBoxType.Info, VisibleIf = "showAlertBox")]
    public bool visibleIfTest;
}
