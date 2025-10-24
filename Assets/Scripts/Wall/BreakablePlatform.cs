using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BreakablePlatform : MonoBehaviour
{
    [Header("파괴 설정")]
    [SerializeField] private float delayBeforeBreak = 1f;     // 부서지기까지 대기
    [SerializeField] private float fallGravityScale = 5f;     // 낙하 시 중력 스케일

    [Header("시각 전용 흔들림")]
    [SerializeField] private Transform visualRoot;            
    [SerializeField] private float shakeIntensity = 0.1f;     
    [SerializeField] private float shakeSpeed = 20f;          
    [SerializeField] private float shakeRotIntensity = 2f;    

    [Header("플레이어 감지 (충돌 트리거 옵션)")]
    [SerializeField] private bool enableCollisionTrigger = true;
    [SerializeField] private bool detectByTag = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask playerLayer; // 리스폰 보류 체크에도 사용 권장(플레이어 레이어만 포함)

    [Header("재생성")]
    [SerializeField] private bool respawn = true;
    [SerializeField] private float respawnDelay = 3f;

    [Tooltip("리스폰 위치에 플레이어가 있으면, 이 간격으로 재시도")]
    [SerializeField] private float respawnCheckInterval = 0.05f;

    [Tooltip("플레이어가 영역을 벗난 뒤, 실제 생성까지 기다릴 지연")]
    [SerializeField] private float respawnAfterClearDelay = 0.2f;

    private Vector3 originalWorldPos;
    private float originalAngleZ;
    private Vector3 visualOriginalLocalPos;
    private Quaternion visualOriginalLocalRot;

    private bool isTriggered = false;
    private bool isFalling = false;
    private bool isRespawning = false; // 중복 코루틴 방지

    private Rigidbody2D rb;
    private BoxCollider2D platformCollider;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        platformCollider = GetComponent<BoxCollider2D>();

        if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.freezeRotation = true;

        if (visualRoot == null)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) visualRoot = sr.transform;
        }
        if (visualRoot == null)
        {
            var go = new GameObject("Visual");
            go.transform.SetParent(transform, false);
            visualRoot = go.transform;
        }

        spriteRenderer = visualRoot.GetComponentInChildren<SpriteRenderer>();

        originalWorldPos = transform.position;
        originalAngleZ = transform.eulerAngles.z;

        visualOriginalLocalPos = visualRoot.localPosition;
        visualOriginalLocalRot = visualRoot.localRotation;
    }

    // 충돌 감지(플레이어가 isTrigger=false BoxCollider2D일 때)
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!enableCollisionTrigger || isTriggered) return;

        bool isPlayer = detectByTag
            ? collision.collider.CompareTag(playerTag)
            : ((playerLayer.value & (1 << collision.collider.gameObject.layer)) != 0);

        if (isPlayer) TriggerBreak();
    }

    /// 외부(레이캐스트 등)에서 실행하는 공식 API
    public void TriggerBreak()
    {
        StartCoroutine(BreakRoutine());
    }

    private IEnumerator BreakRoutine()
    {
        float elapsed = 0f;

        while (elapsed < delayBeforeBreak)
        {
            float t = Time.time * shakeSpeed;
            float x = (Mathf.PerlinNoise(t, 0.123f) * 2f - 1f) * shakeIntensity;
            float y = (Mathf.PerlinNoise(0.456f, t) * 2f - 1f) * shakeIntensity;

            visualRoot.localPosition = visualOriginalLocalPos + new Vector3(x, y, 0f);

            if (shakeRotIntensity > 0f)
            {
                float r = (Mathf.PerlinNoise(t, 0.789f) * 2f - 1f) * shakeRotIntensity;
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, r);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        ResetVisualOnly();
        StartFalling();

        if (respawn)
        {
            // 기존: WaitForSeconds 후 바로 리셋
            // 변경: 영역 비었는지 확인해 조건부 리스폰
            StartRespawnSchedule(respawnDelay);
        }
    }

    private void StartFalling()
    {
        isFalling = true;

        // 맵 밖으로 내려가도록 충돌 제거
        if (platformCollider) platformCollider.enabled = false;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravityScale;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.freezeRotation = false;
    }

    private void StartRespawnSchedule(float initialDelay)
    {
        if (!respawn || isRespawning) return;
        StartCoroutine(RespawnWhenClear(initialDelay));
    }

    // 리스폰 위치에 플레이어가 있으면 대기, 비면 0.2초 후 생성
    private IEnumerator RespawnWhenClear(float initialDelay)
    {
        isRespawning = true;

        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        // 플레이어가 비켜날 때까지 반복 체크
        while (IsPlayerOverlappingRespawnArea())
            yield return new WaitForSeconds(respawnCheckInterval);

        // 비워진 뒤 약간의 안전 지연
        if (respawnAfterClearDelay > 0f)
            yield return new WaitForSeconds(respawnAfterClearDelay);

        ResetPlatformImmediate();
        isRespawning = false;
    }

    // 리스폰 예정 위치의 박스콜라이더 영역을 계산해 플레이어와 겹치는지 확인
    private bool IsPlayerOverlappingRespawnArea()
    {
        if (platformCollider == null) return false;

        GetPlannedRespawnBox(out Vector2 center, out Vector2 size, out float angleDeg);

        // 레이어가 지정되어 있으면 빠르게 체크
        Collider2D hit = Physics2D.OverlapBox(center, size, angleDeg, playerLayer);
        if (hit != null) return true;

        // 태그 기반을 원한다면(레이어를 쓰지 않거나 혼합 환경)
        if (detectByTag)
        {
            var hits = Physics2D.OverlapBoxAll(center, size, angleDeg);
            for (int i = 0; i < hits.Length; i++)
                if (hits[i].CompareTag(playerTag))
                    return true;
        }

        return false;
    }

    // BoxCollider2D의 월드 상 재생성 영역 계산
    private void GetPlannedRespawnBox(out Vector2 center, out Vector2 size, out float angleDeg)
    {
        // 원복 위치에서의 콜라이더 월드 크기/중심을 구성
        Vector3 lossy = transform.lossyScale;

        // offset은 로컬 기준이므로 스케일 반영해서 더한다
        Vector2 worldOffset = new Vector2(platformCollider.offset.x * lossy.x,
                                          platformCollider.offset.y * lossy.y);

        center = (Vector2)originalWorldPos + worldOffset;

        size = new Vector2(
            platformCollider.size.x * Mathf.Abs(lossy.x),
            platformCollider.size.y * Mathf.Abs(lossy.y)
        );

        // 원래 각도 사용(보통 0)
        angleDeg = originalAngleZ;
    }

    // 실제 위치/물리/비주얼 복구. 조건 검사 없이 즉시 리셋한다.
    private void ResetPlatformImmediate()
    {
        // 부모 위치/회전 원복
        transform.position = originalWorldPos;
        transform.rotation = Quaternion.Euler(0, 0, originalAngleZ);

        // 물리 원복
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.freezeRotation = true;

        // 콜라이더 재활성화
        if (platformCollider) platformCollider.enabled = true;

        // 비주얼 원복
        ResetVisualOnly();

        if (spriteRenderer != null)
        {
            var c = spriteRenderer.color;
            c.a = 1f;
            spriteRenderer.color = c;
        }

        isTriggered = false;
        isFalling = false;
    }

    private void ResetVisualOnly()
    {
        visualRoot.localPosition = visualOriginalLocalPos;
        visualRoot.localRotation = visualOriginalLocalRot;
    }

    // 화면 밖으로 나갔을 때도 동일 정책 적용
    void OnBecameInvisible()
    {
        if (!isFalling) return;

        if (respawn)
            StartRespawnSchedule(0.5f); // 예전처럼 살짝 지연 후, '영역 비었는지' 확인
        else
            Destroy(gameObject);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (platformCollider == null) platformCollider = GetComponent<BoxCollider2D>();

        Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.35f);

        // 에디터에서 예정 리스폰 영역을 시각화
        Vector3 lossy = transform.lossyScale;
        Vector2 size = platformCollider
            ? new Vector2(platformCollider.size.x * Mathf.Abs(lossy.x),
                          platformCollider.size.y * Mathf.Abs(lossy.y))
            : Vector2.one * 0.5f;

        Vector2 center = (Vector2)(Application.isPlaying ? originalWorldPos : transform.position);
        if (platformCollider) center += new Vector2(platformCollider.offset.x * lossy.x,
                                                    platformCollider.offset.y * lossy.y);

        // 회전 반영
        Matrix4x4 m = Matrix4x4.TRS(new Vector3(center.x, center.y, 0), 
                                    Quaternion.Euler(0, 0, Application.isPlaying ? originalAngleZ : transform.eulerAngles.z),
                                    Vector3.one);
        Gizmos.matrix = m;
        Gizmos.DrawCube(Vector3.zero, new Vector3(size.x, size.y, 0.01f));
        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}
