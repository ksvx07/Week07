using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] public Camera Cam;
    [SerializeField] private float SmoothTime = 0.2f;
    [SerializeField] public CameraClamp Clamp;
    [SerializeField] private Vector2 minMaxPos;

    [Header("DeadZone / SoftZone")]
    [SerializeField] private Vector2 deadZoneSize = new Vector2(7f, 2f);   // �÷��̾ �� ���� �ȿ� ������ ī�޶� ����
    [SerializeField] private Vector2 softZoneSize = new Vector2(4f, 1f);   // DeadZone�� �Ѿ SoftZone���� �� �� ī�޶� �������ϰ� ����
    [SerializeField] private Vector3 _velocity = new Vector3(4, 4, 4);

    [Header("Zoom In Out")]
    [SerializeField] private float MaxZoomIn = 3f;
    [SerializeField] private float MaxZoomOut = 10f;
    [SerializeField] private float ZoomLerpSpeed = 1f;
    [SerializeField] private float SpeedThreshold = 5f;

    private Transform Player => PlayerManager.Instance?._currentPlayerPrefab?.transform;
    private float targetZoom;
    private Rigidbody2D _rb;
    private bool _forceCentering = false;

    public static CameraController Instance;
    public bool IsTriggerZoom { get; private set; }

    private void Awake()
    {
        if (null == Instance)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        Cam = GetComponent<Camera>();
    }

    private void Start()
    {
        if (Player != null)
            _rb = Player.GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (Player == null) return;

        Vector3 desiredPos;

        if (_forceCentering)
        {
            desiredPos = new Vector3(Player.position.x, Player.position.y, transform.position.z);

            // 줌인 완료 판정 후 강제 모드 해제
            if (Mathf.Abs(Cam.orthographicSize - MaxZoomIn) < 0.05f)
            {
                _forceCentering = false;
            }
        }
        else
        {
            desiredPos = HandleFollow();
        }

        transform.position = Clamp.HandleClamp(desiredPos);
        Vector3 targetClampPos = transform.position;
        targetClampPos.x = Mathf.Clamp(targetClampPos.x, Player.position.x - minMaxPos.x, Player.position.x + minMaxPos.x);
        targetClampPos.y = Mathf.Clamp(targetClampPos.y, Player.position.y - minMaxPos.y, Player.position.y + minMaxPos.y);
        transform.position = targetClampPos;
        if (!IsTriggerZoom)
        {
            HandleZoomInOut();
        }
    }

    private Vector3 HandleFollow()
    {
        Vector3 camPos = transform.position;
        Vector3 playerPos = Player.position;

        Vector2 dz = deadZoneSize;   // DeadZone 크기
        Vector2 sz = softZoneSize;   // SoftZone 크기

        float newX = camPos.x;
        float newY = camPos.y;

        // --- X축 ---
        float deltaX = playerPos.x - camPos.x;
        float absDeltaX = Mathf.Abs(deltaX);

        if (absDeltaX > dz.x + sz.x)
        {
            // DeadZone + SoftZone 밖 → 플레이어 중앙에 오도록 SmoothDamp
            newX = Mathf.SmoothDamp(camPos.x, playerPos.x, ref _velocity.x, SmoothTime);
        }
        else if (absDeltaX > dz.x)
        {
            // DeadZone 밖, SoftZone 안 → 천천히 따라감
            float factor = (absDeltaX - dz.x) / sz.x; // 0~1 비율
            newX += deltaX * factor * 0.1f;           // 이동량 조정
        }
        else
        {
            // DeadZone 안 → 거의 움직이지 않음
            newX += deltaX * 0.05f;
        }

        // --- Y축 ---
        float deltaY = playerPos.y - camPos.y;
        float absDeltaY = Mathf.Abs(deltaY);

        if (absDeltaY > dz.y + sz.y)
        {
            newY = Mathf.SmoothDamp(camPos.y, playerPos.y, ref _velocity.y, SmoothTime);
        }
        else if (absDeltaY > dz.y)
        {
            float factor = (absDeltaY - dz.y) / sz.y;
            newY += deltaY * factor * 0.1f;
        }

        // 맵 Clamp는 여기서 처리하지 않고, Clamp.HandleClamp(desiredPos)에서 적용 가능
        return new Vector3(newX, newY, camPos.z);
    }



    private void HandleCameraShaking()
    {

    }

    private void HandleZoomInOut()
    {
        var playerVelocity = _rb.linearVelocity;
        _velocity = playerVelocity;

        float speed = playerVelocity.magnitude;
        if (speed > SpeedThreshold)
        {
            targetZoom = GetMaxAllowedZoom();
        }
        else
        {
            targetZoom = MaxZoomIn;
        }

        Cam.orthographicSize = Mathf.Lerp(Cam.orthographicSize, targetZoom, Time.deltaTime * ZoomLerpSpeed);
    }

    public void TriggerZoomOut(float targetZoom)
    {
        IsTriggerZoom = true;
        float maxAllowedZoom = GetMaxAllowedZoom();
        float clampedZoom = Mathf.Min(targetZoom, maxAllowedZoom);
        Cam.orthographicSize = Mathf.Lerp(Cam.orthographicSize, clampedZoom, Time.deltaTime * ZoomLerpSpeed);
    }

    public void TriggerZoomIn(float targetZoom)
    {
        _forceCentering = true;

        Cam.orthographicSize = Mathf.Lerp(Cam.orthographicSize, targetZoom, Time.deltaTime * ZoomLerpSpeed);
        IsTriggerZoom = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = new Vector3(transform.position.x, transform.position.y, 0f);
        Vector3 size = new Vector3(deadZoneSize.x * 2f, deadZoneSize.y * 2f, 0f);
        Gizmos.DrawWireCube(center, size);
    }

    private float GetMaxAllowedZoom()
    {
        // Clamp에서 min/max 값 가져오기
        float mapWidth = Clamp._maxX - Clamp._minX;
        float mapHeight = Clamp._maxY - Clamp._minY;

        // 카메라 비율에 따라 최대 zoom 계산
        float maxZoomByWidth = mapWidth / (2f * Cam.aspect);
        float maxZoomByHeight = mapHeight / 2f;

        // 둘 중 작은 값이 실제 최대 zoom
        return Mathf.Min(maxZoomByWidth, maxZoomByHeight, MaxZoomOut);
    }
}
