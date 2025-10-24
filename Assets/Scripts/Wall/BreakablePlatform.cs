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

    [Header("낙하 시 바닥 감지")]
    [SerializeField] private bool useFallDownMode = false; // true: 바닥까지 떨어짐, false: 그냥 떨어져서 사라짐
    [SerializeField] private bool enableGroundDetection = true;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float boxCastDistance = 100f;
    [SerializeField] private GameObject damageColliderObject; // 낙하 중 활성화할 데미지 콜라이더 (자식 오브젝트)

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
    private bool isPlayerDetectionDisabled = false; // 떨어진 후 플레이어 감지 비활성화

    private Rigidbody2D rb;
    private BoxCollider2D platformCollider;
    private SpriteRenderer spriteRenderer;

    // 기즈모 시각화용 히트 정보 저장
    private RaycastHit2D[] lastBoxCastHits = new RaycastHit2D[3];

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
        if (!enableCollisionTrigger || isTriggered || isPlayerDetectionDisabled) return;

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
        isTriggered = true;
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

        // useFallDownMode일 경우 리스폰 방지
        if (respawn && !useFallDownMode)
        {
            StartRespawnSchedule(respawnDelay);
        }
    }

    private void StartFalling()
    {
        isFalling = true;
        isPlayerDetectionDisabled = true; // 플레이어 감지 비활성화

        // 맵 밖으로 내려가도록 충돌 제거
        if (platformCollider) platformCollider.enabled = false;

        if (useFallDownMode && enableGroundDetection)
        {
            // 새로운 방식: 바닥까지 떨어지고 데미지 콜라이더 활성화
            
            // 데미지 콜라이더 활성화
            if (damageColliderObject != null)
            {
                damageColliderObject.SetActive(true);
            }

            // 부드럽게 내려가면서 안착시키는 코루틴 사용
            StartCoroutine(FallAndSettleRoutine());
        }
        else
        {
            // 기존 방식: 단순 중력 적용
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = fallGravityScale;
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0;
            rb.freezeRotation = false;
        }
    }

    /// <summary>
    /// 아래 방향으로 박스캐스트를 수행하여 착지 높이 계산
    /// 플랫폼 바닥에서만 감지 (위의 절벽 무시)
    /// </summary>
    private float CalculateSettleHeight()
    {
        if (platformCollider == null) return transform.position.y;

        float platformHalfHeight = (platformCollider.size.y * transform.localScale.y) * 0.5f;
        
        Vector2 platformCenter = (Vector2)transform.position + new Vector2(
            platformCollider.offset.x * transform.localScale.x,
            platformCollider.offset.y * transform.localScale.y
        );

        // 박스 크기: 좌우는 콜라이더와 동일, 높이는 매우 얇게
        Vector2 boxSize = new Vector2(
            platformCollider.size.x * transform.localScale.x,
            0.05f  // 매우 얇게 (거의 선)
        );

        // 박스캐스트 위치: 플랫폼 바닥에 위치
        Vector2 boxCastPosition = new Vector2(
            platformCenter.x,
            platformCenter.y - platformHalfHeight  // 플랫폼 바닥으로 설정
        );

        // 중앙에서 아래로 박스캐스트
        RaycastHit2D hit = Physics2D.BoxCast(
            boxCastPosition,
            boxSize,
            transform.eulerAngles.z,
            Vector2.down,
            boxCastDistance,
            groundLayer
        );

        // 히트 정보 저장 (기즈모 시각화용)
        lastBoxCastHits[0] = hit;

        if (hit.collider != null)
        {
            // 감지된 게임오브젝트 로그 출력
            Debug.Log($"[BreakablePlatform] 감지된 게임오브젝트: {hit.collider.gameObject.name}, 위치: {hit.point}");

            // 착지 높이 계산
            float settleY = hit.point.y + platformHalfHeight;

#if UNITY_EDITOR
            Debug.Log($"[BreakablePlatform] 착지 높이: {settleY:F2}");
#endif

            return settleY;
        }

        return transform.position.y;
    }

    /// <summary>
    /// 부드럽게 바닥으로 내려가면서 안착시키는 코루틴
    /// Rigidbody의 속도를 제어하여 자연스러운 낙하 표현
    /// </summary>
    private IEnumerator FallAndSettleRoutine()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravityScale;
        rb.freezeRotation = false;

        // 초기 속도 설정 (살짝의 초기 속도)
        rb.linearVelocity = new Vector2(0, -2f);

        float settleHeight = CalculateSettleHeight();
        float settleTolerance = 0.05f;  // 안착 판정 거리
        float maxFallTime = 10f;        // 최대 낙하 시간 (무한 루프 방지)
        float elapsedTime = 0f;

        // 목표 높이에 도달할 때까지 계속 낙하
        while (transform.position.y > settleHeight + settleTolerance)
        {
            elapsedTime += Time.deltaTime;

            // 매 프레임 바닥 높이 재계산 (울퉁불퉁한 지형 추적)
            float currentSettleHeight = CalculateSettleHeight();
            
            // 가까워질수록 속도 감소 (부드러운 착지)
            float distanceToGround = transform.position.y - currentSettleHeight;
            float velocityY = -Mathf.Max(5f, fallGravityScale * Time.deltaTime);
            
            // 거리가 가까워지면 속도 줄이기
            if (distanceToGround < 2f)
            {
                velocityY *= 0.5f;
            }
            if (distanceToGround < 0.5f)
            {
                velocityY *= 0.3f;
            }

            rb.linearVelocity = new Vector2(0, velocityY);

            // 안전 장치: 너무 오래 떨어지고 있으면 강제 안착
            if (elapsedTime > maxFallTime)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[BreakablePlatform] 최대 낙하 시간 초과 - 강제 안착");
#endif
                break;
            }

            yield return null;
        }

        // 안착 완료
        FinalizeSettle();
    }

    /// <summary>
    /// 최종 안착 처리
    /// </summary>
    private void FinalizeSettle()
    {
        // 최종 높이 설정
        float finalHeight = CalculateSettleHeight();
        transform.position = new Vector3(transform.position.x, finalHeight, transform.position.z);

        // useFallDownMode일 경우만 콜라이더 처리
        if (useFallDownMode)
        {
            // 리지드바디를 Kinematic으로 변경 (플레이어에게 밀리지 않도록)
            rb.bodyType = RigidbodyType2D.Kinematic;

            // 데미지 콜라이더 비활성화
            if (damageColliderObject != null)
            {
                damageColliderObject.SetActive(false);
            }

            // 기존 박스 콜라이더 원복 (새로운 땅으로 기능)
            if (platformCollider)
            {
                platformCollider.enabled = true;
            }
        }

        // 물리 상태 정리
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.gravityScale = 0;
        rb.freezeRotation = true;

#if UNITY_EDITOR
        Debug.Log($"[BreakablePlatform] 안착 완료 - 최종 위치: {transform.position.y:F2}");
#endif
    }

    /// <summary>
    /// 기존 메서드 - 필요시 참고용으로 보존 (기존 방식)
    /// </summary>
    [System.Obsolete("FallAndSettleRoutine 사용 권장")]
    private void CalculateAndMoveToPlatformPosition()
    {
        if (platformCollider == null) return;

        Vector2 boxSize = new Vector2(
            platformCollider.size.x * transform.localScale.x,
            platformCollider.size.y * transform.localScale.y * 0.1f
        );

        Vector2 boxCenter = (Vector2)transform.position + new Vector2(
            platformCollider.offset.x * transform.localScale.x,
            platformCollider.offset.y * transform.localScale.y
        );

        RaycastHit2D hit = Physics2D.BoxCast(
            boxCenter,
            boxSize,
            transform.eulerAngles.z,
            Vector2.down,
            boxCastDistance,
            groundLayer
        );

        if (hit.collider != null)
        {
            float platformHalfHeight = (platformCollider.size.y * transform.localScale.y) * 0.5f;
            float targetY = hit.point.y + platformHalfHeight;
            
            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);

#if UNITY_EDITOR
            Debug.Log($"[BreakablePlatform] 바닥 감지 완료 - Hit Point: {hit.point}, Target Y: {targetY}");
#endif
        }
        else
        {
#if UNITY_EDITOR
            Debug.LogWarning($"[BreakablePlatform] 바닥을 감지하지 못함. groundLayer 설정을 확인하세요.");
#endif
        }
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

        // 데미지 콜라이더 비활성화
        if (damageColliderObject != null)
        {
            damageColliderObject.SetActive(false);
        }

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
        isPlayerDetectionDisabled = false; // 플레이어 감지 재활성화
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

        // 데미지 콜라이더 비활성화
        if (damageColliderObject != null)
        {
            damageColliderObject.SetActive(false);
        }

        // useFallDownMode일 경우 리스폰 방지
        if (respawn && !useFallDownMode)
            StartRespawnSchedule(0.5f);
        else if (!useFallDownMode)
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

        // 박스캐스트 시각화 (실시간 편집에서)
        if (enableGroundDetection && Application.isPlaying && isFalling)
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
            float platformHalfHeight = (platformCollider.size.y * lossy.y) * 0.5f;
            Vector2 boxSize = new Vector2(
                platformCollider.size.x * lossy.x,
                0.05f  // 매우 얇게
            );
            Vector2 boxCenter = (Vector2)transform.position + new Vector2(
                platformCollider.offset.x * lossy.x,
                platformCollider.offset.y * lossy.y
            );
            // 박스캐스트 위치: 플랫폼 바닥
            Vector2 boxCastPosition = new Vector2(
                boxCenter.x,
                boxCenter.y - platformHalfHeight
            );
            m = Matrix4x4.TRS(new Vector3(boxCastPosition.x, boxCastPosition.y, 0), 
                            Quaternion.Euler(0, 0, transform.eulerAngles.z),
                            Vector3.one);
            Gizmos.matrix = m;
            Gizmos.DrawCube(Vector3.zero, new Vector3(boxSize.x, boxSize.y, 0.01f));
            Gizmos.matrix = Matrix4x4.identity;
        }

        // 박스캐스트 히트 지점 시각화 (노란색 구)
        if (enableGroundDetection && Application.isPlaying && isFalling)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 1f); // 노란색
            for (int i = 0; i < lastBoxCastHits.Length; i++)
            {
                if (lastBoxCastHits[i].collider != null)
                {
                    // 히트 지점에 구 그리기
                    Gizmos.DrawSphere(lastBoxCastHits[i].point, 0.1f);
                    
                    // 히트 지점에서 법선 방향으로 선 그리기
                    Gizmos.color = new Color(0f, 1f, 1f, 1f); // 하늘색
                    Gizmos.DrawLine(lastBoxCastHits[i].point, lastBoxCastHits[i].point + lastBoxCastHits[i].normal * 0.3f);
                }
            }
        }
    }
#endif
}