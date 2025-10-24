using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class MapDataLog : MonoBehaviour
{
    [Header("구역 이름")]
    [Tooltip("로그에 표시될 이 구역의 이름입니다.")]
    [SerializeField]
    private string zoneName = "Unnamed Zone";

    [Header("금지 구역 여부")]
    [Tooltip("금지 구역으로 설정")]
    [SerializeField]
    private bool isForbiddenZone;

    [Header("예상 도형 여부")]
    [Tooltip("특정 플레이어 도형을 기대하는 구역으로 설정")]
    [SerializeField]
    private bool isExpectedZone;

    [Tooltip("기대하는 플레이어의 도형")]
    [ShowIf("isExpectedZone")]
    [InfoBox("금지 구역에는 예상 도형이 적용 되지 않습니다!", InfoBoxType.Error, VisibleIf = "isForbiddenZone")]
    [SerializeField]
    private PlayerShape expectShape;


    private PlayerShape shapeOnEnter;
    private bool isPlayerInZone = false; // 플레이어의 현재 존 진입 상태
    private Coroutine exitCheckCoroutine; // 퇴장 확인 코루틴

    #region 콜라이더 변수
    private BoxCollider2D boxCollider;
    private Vector2 savedSize; // 크기를 저장할 변수
    #endregion
    void OnValidate()
    {
        if (boxCollider == null)
        {
            boxCollider = GetComponent<BoxCollider2D>();
        }
    }
    private void OnDisable()
    {
        GetComponent<BoxCollider2D>().enabled = false;
    }

    // 스크립트가 활성화되면 콜라이더도 다시 활성화합니다.
    private void OnEnable()
    {
        GetComponent<BoxCollider2D>().enabled = true;
    }


    /// <summary>
    /// 다른 콜라이더가 이 오브젝트의 트리거 안으로 들어왔을 때 호출됩니다.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 이미 플레이어가 진입했으면 체크 하지 않는다
        if (!other.CompareTag("Player"))
        {
            return;
        }

        if (other.transform.parent == null)
        {
            return;
        }

        if (exitCheckCoroutine != null)
        {
            StopCoroutine(exitCheckCoroutine);
            exitCheckCoroutine = null;
        }

        // 플레이어가 존에 처음 들어온 것이라면 진입 로그를 남깁니다.
        if (!isPlayerInZone)
        {
            isPlayerInZone = true;
            LogEntry(); // 로그 기록 로직을 별도 함수로 호출
        }
    }


    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || other.transform.parent == null)
        {
            return;
        }
        // 퇴장 확인 코루틴을 시작합니다.
        exitCheckCoroutine = StartCoroutine(DelayedExitCheck());
    }

    private IEnumerator DelayedExitCheck()
    {
        // 딱 1프레임만 기다려서 폼 변경인지 진짜 퇴장인지 확인합니다.
        yield return new WaitForSeconds(0.2f);

        // 코루틴이 중단되지 않았다면 진짜 퇴장입니다.
        isPlayerInZone = false;
        exitCheckCoroutine = null;
        LogExit(); // 로그 기록 로직을 별도 함수로 호출
    }


    /// <summary>
    /// 플레이어가 존에 진입했을 때 상세 로그를 기록합니다.
    /// </summary>
    private void LogEntry()
    {
        shapeOnEnter = PlayerManager.Instance.CurrentShape;

        // 금지 구역에 들어왔는지 확인
        if (isForbiddenZone)
        {
            GameLog.Warn($"'{zoneName}' 금지 구역 진입! /도형: {shapeOnEnter}", this);
            return;
        }

        // 특정 도형을 기대하는 구역인지 확인
        if (isExpectedZone && shapeOnEnter != expectShape)
        {
            GameLog.Warn($"'{zoneName}' 구역 잘못된 도형으로 진입! / 현재: {shapeOnEnter}, 예상: {expectShape}", this);
            return;
        }

        // 위 모든 특수 케이스에 해당하지 않으면 일반적인 진입으로 처리
        GameLog.Info($"'{zoneName}' 구역 진입 / 도형: {shapeOnEnter}", this);
    }

    /// <summary>
    /// 플레이어가 존에서 이탈했을 때 상세 로그를 기록합니다.
    /// </summary>
    private void LogExit()
    {
        PlayerShape shapeOnExit = PlayerManager.Instance.CurrentShape;
        string message = $"'{zoneName}' 구역 이탈 / 도형 : {shapeOnEnter}";

        // 구역 내에서 도형이 변경되었는지 확인하고 메시지에 추가합니다.
        if (shapeOnEnter != shapeOnExit)
        {
            message += $" -> {shapeOnExit}";
        }

        // 금지 구역이었는지 여부에 따라 로그 레벨만 결정합니다.
        if (isForbiddenZone)
        {
            GameLog.Warn(message, this);
        }
        else
        {
            GameLog.Info(message, this);
        }
    }

    /// <summary>
    /// 콜라이더 크기를 (1, 1)로 설정합니다.
    /// </summary>
    [Button("초기화")]
    public void SetSizeToOneOne()
    {
        if (boxCollider != null)
        {
            boxCollider.size = new Vector2(1f, 1f);
        }
    }

    /// <summary>
    /// 콜라이더의 가로(X) 크기만 1 증가시킵니다.
    /// </summary>
    [Button("X축 증가")]
    public void IncreaseX()
    {
        if (boxCollider != null)
        {
            // 현재 크기를 가져와서 x값에 1을 더한 새로운 Vector2를 만듭니다.
            boxCollider.size = new Vector2(boxCollider.size.x + 1f, boxCollider.size.y);
        }
    }

    /// <summary>
    /// 콜라이더의 세로(Y) 크기만 1 증가시킵니다.
    /// </summary>
    [Button("Y축 증가")]
    public void IncreaseY()
    {
        if (boxCollider != null)
        {
            // 현재 크기를 가져와서 y값에 1을 더한 새로운 Vector2를 만듭니다.
            boxCollider.size = new Vector2(boxCollider.size.x, boxCollider.size.y + 1f);
        }
    }

    /// <summary>
    /// 현재 콜라이더의 크기를 변수에 저장합니다.
    /// </summary>
    [Button("현재 범위 저장")]
    public void SaveCurrentSize()
    {
        if (boxCollider != null)
        {
            savedSize = boxCollider.size;
            Debug.Log("현재 크기 저장됨: " + savedSize);
        }
    }

    /// <summary>
    /// 저장해뒀던 크기로 콜라이더를 되돌립니다.
    /// </summary>
    [Button("저장 범위 복원")]
    public void RevertToSavedSize()
    {
        if (boxCollider != null)
        {
            boxCollider.size = savedSize;
            Debug.Log("저장된 크기로 복원됨: " + savedSize);
        }
    }

}
