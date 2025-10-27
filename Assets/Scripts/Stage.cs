using UnityEngine;

public enum SwitchDirection
{
    LeftAndRight, // 좌우 방향을 하나로 통합
    TopToBottom,  // 위에서 아래로
    BottomToTop  // 아래서 위로
}
public class Stage : MonoBehaviour
{
    [Header("스테이지 데이터")]
    [Tooltip("Trigger 통과 시 넘어가는 다음 스테이지 정보")]
    [SerializeField] private StageScriptableObject stageData;

    [Header("진행 방향")]
    [SerializeField] private SwitchDirection boundaryDirection;

    // 플레이어가 트리거 영역을 나갈 때 한 번만 호출됨
    private void OnTriggerExit2D(Collider2D collision)
    {
        // 나간 오브젝트가 플레이어가 아니거나, 소속된 스테이지가 없으면 무시
        if (!collision.CompareTag("Player")) return;
        if (stageData == null)
        {
            Debug.LogError("StageSwitch에 myStageSO가 할당되지 않았습니다!", gameObject);
            return;
        }

        Transform player = collision.transform;
        bool shouldGoNext; // 다음 스테이지로 가야하는가? (true = 다음, false = 이전)

        // 경계 방향에 따라 플레이어의 이동 방향을 판단합니다.
        switch (boundaryDirection)
        {
            case SwitchDirection.LeftAndRight:
                // 플레이어의 x좌표가 경계의 중심보다 크면 '오른쪽'으로 나간 것.
                // 오른쪽을 '다음(Next)' 방향으로 간주합니다.
                shouldGoNext = player.position.x > transform.position.x;
                break;

            case SwitchDirection.BottomToTop:
                // 플레이어의 y좌표가 경계의 중심보다 크면 '위쪽'으로 나간 것.
                // 위쪽을 '다음(Next)' 방향으로 간주합니다.
                shouldGoNext = player.position.y > transform.position.y;
                break;
            case SwitchDirection.TopToBottom:
                // 플레이어의 y좌표가 경계의 중심보다 크면 '위쪽'으로 나간 것.
                // 위쪽을 '다음(Next)' 방향으로 간주합니다.
                shouldGoNext = player.position.y < transform.position.y;
                break;

            default:
                // 혹시 모를 예외 상황 방지
                return;
        }

        // 최종적으로 판단된 결과를 StageManager에게 요청합니다.
        Debug.Log($"'{stageData.name}' 스테이지에서 {(shouldGoNext ? "다음" : "이전")} 방향으로 이동 요청!");
        StageManager.Instance.RequestStageChange(stageData, shouldGoNext);
    }


    // Hack: 제거하기
    [Button("디버그용 순간이동")]
    private void DebugPlayerTranform()
    {
        // 게임이 실행 중일 때만 아래 코드를 실행합니다.
        if (Application.isPlaying)
        {
            Transform player = PlayerManager.Instance?._currentPlayerPrefab?.transform;

            // player가 null이 아닐 때만 위치를 변경하여 에러를 방지합니다.
            if (player != null)
            {
                player.position = transform.position;
            }
        }
    }
}
