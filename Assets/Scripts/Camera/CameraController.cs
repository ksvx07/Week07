using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] public Camera Cam;
    [SerializeField] private float SmoothTime = 0.2f;
    [SerializeField] public CameraClamp Clamp;
    [SerializeField] private Vector2 minMaxPos;

    [Header("DeadZone / SoftZone")]
    [SerializeField] private Vector2 deadZoneSize = new Vector2(7f, 2f);
    [SerializeField] private Vector2 softZoneSize = new Vector2(4f, 1f);
    [SerializeField] private Vector3 _velocity = new Vector3(4, 4, 4);

    private float ZoomLerpSpeed = 1f;

    private Transform Player => PlayerManager.Instance?._currentPlayerPrefab?.transform;
    private float targetZoom;

    public static CameraController Instance;
    public bool IsTriggerZoom { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Cam = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (Player == null || Clamp == null) return;

        // 플레이어 중심으로 이동하려는 목표 위치 계산
        Vector3 desiredPos = HandleFollow();

        // Clamp 적용 (맵 경계 안으로 제한)
        desiredPos = Clamp.HandleClamp(desiredPos);

        // 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, desiredPos, Time.deltaTime * (1f / SmoothTime));

        transform.position = desiredPos;
        Vector3 targetClampPos = transform.position;
        targetClampPos.x = Mathf.Clamp(targetClampPos.x, Player.position.x - minMaxPos.x, Player.position.x + minMaxPos.x);
        targetClampPos.y = Mathf.Clamp(targetClampPos.y, desiredPos.y - minMaxPos.y, desiredPos.y + minMaxPos.y);
        transform.position = targetClampPos;

        // 줌 처리
        Cam.orthographicSize = Mathf.Lerp(Cam.orthographicSize, targetZoom, Time.deltaTime * ZoomLerpSpeed);
    }


    private Vector3 HandleFollow()
    {
        Vector3 camPos = transform.position;
        Vector3 playerPos = Player.position;

        float newX = camPos.x;
        float newY = camPos.y;

        // --- X축 ---
        float deltaX = playerPos.x - camPos.x;
        float absDeltaX = Mathf.Abs(deltaX);

        if (absDeltaX > deadZoneSize.x + softZoneSize.x)
        {
            // DeadZone + SoftZone 밖 = 플레이어 중앙에 오도록 SmoothDamp
            newX = Mathf.SmoothDamp(camPos.x, playerPos.x, ref _velocity.x, SmoothTime);
        }
        else if (absDeltaX > deadZoneSize.x)
        {
            // DeadZone 밖, SoftZone 안 = 천천히 따라감
            float factor = (absDeltaX - deadZoneSize.x) / softZoneSize.x; // 0~1 비율
            newX += deltaX * factor * 0.1f; 
        }
        else
        {
            // DeadZone 안 = 거의 움직이지 않음
            newX += deltaX * 0.05f;
        }

        // --- Y축 ---
        float deltaY = playerPos.y - camPos.y;
        float absDeltaY = Mathf.Abs(deltaY);

        if (absDeltaY > deadZoneSize.y + softZoneSize.y)
        {
            newY = Mathf.SmoothDamp(camPos.y, playerPos.y, ref _velocity.y, SmoothTime);
        }
        else if (absDeltaY > deadZoneSize.y)
        {
            float factor = (absDeltaY - deadZoneSize.y) / deadZoneSize.y;
            newY += deltaY * factor * 0.1f;
        }

        return new Vector3(newX, newY, camPos.z);
    }

    public void TriggerZoom(float targetZoom)
    {
        this.targetZoom = targetZoom;
        IsTriggerZoom = true; 
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(transform.position.x, transform.position.y, 0f);
        Vector3 size = new Vector3(deadZoneSize.x * 2f, deadZoneSize.y * 2f, 0f);
        Gizmos.DrawWireCube(center, size);
    }
}
