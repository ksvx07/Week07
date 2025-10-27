using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BreakablePlatform : MonoBehaviour
{
    /// <summary>
    /// 플랫폼 낙하 모드 정의
    /// </summary>
    public enum FallMode
    {
        [Tooltip("화면 밖으로 떨어져 사라짐")]
        FreeFall = 0,
        
        [Tooltip("바닥까지 낙하 후 새로운 플랫폼으로 안착")]
        SettleOnGround = 1
    }

    [Header("파괴 설정")]
    [SerializeField] private float delayBeforeBreak = 1f;
    [SerializeField] private float fallGravityScale = 5f;

    [Header("시각 전용 흔들림")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float shakeIntensity = 0.1f;
    [SerializeField] private float shakeSpeed = 20f;
    [SerializeField] private float shakeRotIntensity = 2f;

    [Header("플레이어 감지 (충돌 트리거 옵션)")]
    [SerializeField] private bool enableCollisionTrigger = true;
    [SerializeField] private bool detectByTag = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private LayerMask playerLayer;

    [Header("낙하 모드")]
    [Tooltip("FreeFall: 화면 밖으로 떨어져 사라짐\nSettleOnGround: 바닥까지 낙하 후 새로운 플랫폼으로 안착")]
    [SerializeField] private FallMode fallMode = FallMode.FreeFall;
    
    [Tooltip("바닥 감지 활성화 (SettleOnGround 모드에서만 사용)")]
    [SerializeField] private bool enableGroundDetection = true;
    
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float boxCastDistance = 100f;
    
    [Space(10)]
    [Header("데미지 콜라이더 (낙하 중 플레이어 충돌 처리)")]
    [SerializeField] private GameObject damageColliderObject;
    
    [Tooltip("낙하 시작 시 데미지 콜라이더 활성화 여부 (모든 낙하 모드 공통 적용)")]
    [SerializeField] private bool enableDamageColliderOnFall = true;

    [Header("재생성")]
    [SerializeField] private bool respawn = true;
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private float respawnCheckInterval = 0.05f;
    [SerializeField] private float respawnAfterClearDelay = 0.2f;
    [SerializeField] private bool respawnWhenPlayerRespawns = true;

    private Vector3 originalWorldPos;
    private float originalAngleZ;
    private Vector3 visualOriginalLocalPos;
    private Quaternion visualOriginalLocalRot;

    // 초기 옵션값 저장
    private bool originalEnableCollisionTrigger;
    private bool originalDetectByTag;
    private FallMode originalFallMode;
    private bool originalEnableGroundDetection;
    private bool originalRespawn;

    // 모드별 데미지 콜라이더 활성화 여부 (내부 관리용, 직렬화 안 함)
    private bool enableDamageColliderOnSettleMode;
    private bool enableDamageColliderOnFreeFall;
    private bool originalEnableDamageColliderOnFall;

    private bool isTriggered = false;
    private bool isFalling = false;
    private bool isRespawning = false;
    private bool isPlayerDetectionDisabled = false;

    // 내부적으로 사용할 useFallDownMode (enum 기반으로 자동 결정)
    private bool UseFallDownMode => fallMode == FallMode.SettleOnGround;

    private Rigidbody2D rb;
    private BoxCollider2D platformCollider;
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        // 통합 bool 값을 각 모드별 내부 변수에 적용
        enableDamageColliderOnSettleMode = enableDamageColliderOnFall;
        enableDamageColliderOnFreeFall = enableDamageColliderOnFall;
        
        InitializeComponents();
    }

    void Start()
    {
        SubscribeToRespawnManager();
    }

    void OnDestroy()
    {
        UnsubscribeFromRespawnManager();
    }

    /// <summary>
    /// RespawnManager의 OnPlayerSpawned 이벤트 구독
    /// 플레이어가 리스폰될 때 플랫폼을 초기화
    /// </summary>
    private void SubscribeToRespawnManager()
    {
        var respawnManager = FindFirstObjectByType<RespawnManager>();
        if (respawnManager != null)
        {
            respawnManager.OnPlayerSpawned += OnPlayerRespawned;
        }
    }

    /// <summary>
    /// RespawnManager의 OnPlayerSpawned 이벤트 구독 해제
    /// 메모리 누수 방지
    /// </summary>
    private void UnsubscribeFromRespawnManager()
    {
        var respawnManager = FindFirstObjectByType<RespawnManager>();
        if (respawnManager != null)
        {
            respawnManager.OnPlayerSpawned -= OnPlayerRespawned;
        }
    }

    /// <summary>
    /// 플레이어 리스폰 시 호출되는 콜백
    /// 수도코드:
    /// 1. 실행 중인 모든 코루틴 중지
    /// 2. 플랫폼 즉시 초기화
    /// </summary>
    private void OnPlayerRespawned(Vector3 spawnPosition)
    {
        if (!respawnWhenPlayerRespawns)
            return;
        StopAllCoroutines();
        ResetPlatformImmediate();
    }

    /// <summary>
    /// 모든 컴포넌트 초기화 (Rigidbody, BoxCollider, Visual 등)
    /// 런타임 중 상태 초기화가 필요할 때 재호출 가능
    /// </summary>
    private void InitializeComponents()
    {
        InitializeRigidbody();
        InitializeVisualRoot();
        CacheOriginalState();
    }

    /// <summary>
    /// Rigidbody2D 컴포넌트 설정
    /// 없으면 생성, Kinematic 상태로 고정
    /// </summary>
    private void InitializeRigidbody()
    {
        var _rb = GetComponent<Rigidbody2D>();

        if (_rb == null)
            _rb = gameObject.AddComponent<Rigidbody2D>();

        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.gravityScale = 0;
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0;
        _rb.freezeRotation = true;

        rb = _rb;
    }

    /// <summary>
    /// Visual 오브젝트(흔들림 표현용) 트랜스폼 초기화
    /// visualRoot가 없으면 자식 오브젝트 생성
    /// </summary>
    private void InitializeVisualRoot()
    {
        if (visualRoot == null)
        {
            var _spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (_spriteRenderer != null)
                visualRoot = _spriteRenderer.transform;
        }

        if (visualRoot == null)
        {
            var _go = new GameObject("Visual");
            _go.transform.SetParent(transform, false);
            visualRoot = _go.transform;
        }

        spriteRenderer = visualRoot.GetComponentInChildren<SpriteRenderer>();
        platformCollider = GetComponent<BoxCollider2D>();
    }

    /// <summary>
    /// 초기 위치, 각도, Visual 상태, 옵션값 저장
    /// 리스폰 시 원본 상태로 복구하기 위함
    /// </summary>
    private void CacheOriginalState()
    {
        // 위치 및 회전 저장
        originalWorldPos = transform.position;
        originalAngleZ = transform.eulerAngles.z;
        
        // Visual 상태 저장
        visualOriginalLocalPos = visualRoot.localPosition;
        visualOriginalLocalRot = visualRoot.localRotation;
        
        // bool 옵션 초기값 저장
        originalEnableCollisionTrigger = enableCollisionTrigger;
        originalDetectByTag = detectByTag;
        originalFallMode = fallMode;
        originalEnableGroundDetection = enableGroundDetection;
        originalEnableDamageColliderOnFall = enableDamageColliderOnFall;
        originalRespawn = respawn;
    }

    // 플레이어 충돌 감지 (isTrigger=false인 BoxCollider2D)
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!enableCollisionTrigger || isTriggered || isPlayerDetectionDisabled)
            return;

        bool _isPlayer = detectByTag
            ? collision.collider.CompareTag(playerTag)
            : ((playerLayer.value & (1 << collision.collider.gameObject.layer)) != 0);

        if (_isPlayer)
            TriggerBreak();
    }

    /// <summary>
    /// 외부 인터페이스: 플랫폼 파괴 시작
    /// 레이캐스트 등 외부에서 호출 가능
    /// </summary>
    public void TriggerBreak()
    {
        // 이미 트리거되었거나 현재 떨어지고 있으면 다시 실행하지 않음
        if (isTriggered || isFalling)
            return;
        
        StartCoroutine(BreakRoutine());
    }

    /// <summary>
    /// 파괴 루틴: 흔들림 -> 낙하 -> 리스폰 스케줄
    /// 수도코드:
    /// 1. isTriggered 플래그 활성화
    /// 2. delayBeforeBreak 동안 Visual에 Perlin 노이즈 흔들림 효과 적용
    /// 3. 흔들림 종료 후 Visual 원상복구
    /// 4. 낙하 시작
    /// 5. FreeFall 모드면 respawnDelay 후 리스폰 시작
    /// </summary>
    private IEnumerator BreakRoutine()
    {
        isTriggered = true;
        float _elapsed = 0f;

        // 흔들림 효과: Perlin 노이즈로 부자연스럽지 않은 흔들림 생성
        while (_elapsed < delayBeforeBreak)
        {
            float _t = Time.time * shakeSpeed;
            float _x = (Mathf.PerlinNoise(_t, 0.123f) * 2f - 1f) * shakeIntensity;
            float _y = (Mathf.PerlinNoise(0.456f, _t) * 2f - 1f) * shakeIntensity;

            visualRoot.localPosition = visualOriginalLocalPos + new Vector3(_x, _y, 0f);

            // 회전 흔들림 (shakeRotIntensity > 0일 때만)
            if (shakeRotIntensity > 0f)
            {
                float _r = (Mathf.PerlinNoise(_t, 0.789f) * 2f - 1f) * shakeRotIntensity;
                visualRoot.localRotation = Quaternion.Euler(0f, 0f, _r);
            }

            _elapsed += Time.deltaTime;
            yield return null;
        }

        ResetVisualOnly();
        StartFalling();

        // FreeFall 모드일 때 respawnDelay 후 리스폰
        if (respawn && fallMode == FallMode.FreeFall)
        {
            yield return new WaitForSeconds(respawnDelay);
            StartCoroutine(RespawnWhenClear(0f));
        }
    }

    /// <summary>
    /// 낙하 상태로 전환
    /// 수도코드:
    /// 1. isFalling, isPlayerDetectionDisabled 플래그 활성화
    /// 2. platformCollider 비활성화 (충돌 방지)
    /// 3. 데미지 콜라이더 활성화 (낙하 시작 시점)
    /// 4. fallMode에 따라 낙하 방식 결정:
    ///    - SettleOnGround: FallAndSettleRoutine으로 바닥까지 낙하
    ///    - FreeFall: Rigidbody Dynamic으로 자유 낙하
    /// </summary>
    private void StartFalling()
    {
        isFalling = true;
        isPlayerDetectionDisabled = true;

        if (platformCollider)
            platformCollider.enabled = false;

        // 낙하 시작 시점에 데미지 콜라이더 활성화
        bool shouldEnableDamage = fallMode == FallMode.SettleOnGround
            ? enableDamageColliderOnSettleMode
            : enableDamageColliderOnFreeFall;

        if (shouldEnableDamage && damageColliderObject != null)
            damageColliderObject.SetActive(true);

        // Rigidbody 설정
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravityScale;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.freezeRotation = false;

        // 바닥 안착 모드일 경우 FallAndSettleRoutine 실행
        if (fallMode == FallMode.SettleOnGround && enableGroundDetection)
        {
            StartCoroutine(FallAndSettleRoutine());
        }
    }

    /// <summary>
    /// 바닥 감지를 위한 박스캐스트 계산(최종 정착 '센터 Y'를 한 번만 산출)
    /// - 월드 스케일을 고려하여 실제 박스 크기 계산
    /// - 자신의 중심에서 아래로 박스캐스트를 쏘기 (콜라이더 오프셋 반영)
    /// - 가로는 자신과 같음, 높이는 매우 얇게 설정 (정확한 지면 감지)
    /// - 위를 향한 면(normal.y > 0.2f)만 착지면으로 인정
    /// </summary>
    private float CalculateSettleHeight()
    {
        if (platformCollider == null)
            return transform.position.y;

        // 월드 스케일을 고려한 박스컬라이더 실제 크기 계산
        Vector3 _lossy = transform.lossyScale;
        Vector2 _colliderWorldSize = new Vector2(
            platformCollider.size.x * Mathf.Abs(_lossy.x),
            platformCollider.size.y * Mathf.Abs(_lossy.y)
        );

        // 월드 콜라이더 오프셋 계산
        Vector2 _worldOffset = new Vector2(
            platformCollider.offset.x * _lossy.x,
            platformCollider.offset.y * _lossy.y
        );

        // 박스캐스트 시작점: 자신의 중심 + 콜라이더 오프셋
        Vector2 _origin = (Vector2)transform.position + _worldOffset;

        // 박스 크기: 가로는 자신과 같음, 높이는 매우 얇게 (정확한 지면 감지용)
        Vector2 _size = new Vector2(_colliderWorldSize.x * 0.98f, 0.1f);

        RaycastHit2D _hit = Physics2D.BoxCast(
            _origin,
            _size,
            0f,
            Vector2.down,
            boxCastDistance,
            groundLayer
        );

        if (_hit.collider != null && _hit.normal.y > 0.2f)
        {
            // 지면의 윗면에 플랫폼의 중심이 오도록 설정
            // 최종 센터 Y = 지면 표면 + 플랫폼 높이의 절반
            return _hit.point.y + (_colliderWorldSize.y / 2f);
        }

        // 히트가 없으면 현재 위치 유지
        return transform.position.y;
    }


    /// <summary>
    /// 바닥까지 부드럽게 낙하 후 안착하는 루틴
    /// 수도코드:
    /// 1. Rigidbody를 Dynamic으로 전환, 중력 적용
    /// 2. 초기 착지 높이 계산
    /// 3. 현재 위치가 착지 높이보다 위에 있는 동안 중력으로 낙하
    /// 4. 착지점에 도달할 때까지 중력으로 낙하
    /// 5. 최종 안착 처리
    /// </summary>
    private IEnumerator FallAndSettleRoutine()
    {
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = fallGravityScale;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.freezeRotation = false;

        // 초기 착지 높이 계산
        float _settleHeight = CalculateSettleHeight();

        // 현재 위치가 착지 높이보다 위에 있는 동안 중력으로 낙하
        while (transform.position.y > _settleHeight)
        {
            yield return null;
        }

        float _maxFallTime = 10f;
        float _elapsedTime = 0f;

        // 착지점에 도달할 때까지 중력으로 낙하
        while (transform.position.y > _settleHeight)
        {
            _elapsedTime += Time.deltaTime;

            // 안전 장치: 너무 오래 떨어지고 있으면 강제 안착
            if (_elapsedTime > _maxFallTime)
            {
                break;
            }

            yield return null;
        }

        // 안착 완료
        FinalizeSettle();
    }

    /// <summary>
    /// 최종 안착 처리: 위치 확정, Rigidbody 복구, 콜라이더 관리
    /// useFallDownMode == true일 때만 호출됨
    /// 수도코드:
    /// 1. 최종 착지 높이 계산 후 위치 설정
    /// 2. Rigidbody를 Kinematic으로 복구
    /// 3. 데미지 콜라이더 비활성화 (착지 완료)
    /// 4. platformCollider 재활성화 (새로운 땅으로 기능)
    /// 5. 낙하 종료 플래그 설정
    /// </summary>
    private void FinalizeSettle()
    {
        // 최종 높이 설정
        float _finalHeight = CalculateSettleHeight();
        transform.position = new Vector3(transform.position.x, _finalHeight, transform.position.z);

        // Rigidbody 복구 (플레이어에게 밀리지 않도록 Kinematic)
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.freezeRotation = true;

        // 데미지 콜라이더 비활성화 (착지 완료했으므로)
        if (damageColliderObject != null)
            damageColliderObject.SetActive(false);

        // 기존 박스 콜라이더 원복 (새로운 땅으로 기능)
        if (platformCollider)
            platformCollider.enabled = true;

        isFalling = false;
    }

    /// <summary>
    /// 리스폰 스케줄 시작
    /// 수도코드:
    /// 1. respawn이 true이고 이미 리스폰 중이 아니면 RespawnWhenClear 코루틴 시작
    /// </summary>
    private void StartRespawnSchedule(float _initialDelay)
    {
        if (!respawn || isRespawning)
            return;

        StartCoroutine(RespawnWhenClear(_initialDelay));
    }

    /// <summary>
    /// 리스폰 대기 루틴: 플레이어가 리스폰 영역을 벗어날 때까지 대기
    /// 수도코드:
    /// 1. isRespawning 플래그 활성화
    /// 2. initialDelay 시간 대기
    /// 3. 플레이어가 리스폰 영역과 겹칠 때까지 respawnCheckInterval마다 체크
    /// 4. 플레이어 이동 후 respawnAfterClearDelay 추가 대기
    /// 5. ResetPlatformImmediate() 호출
    /// 6. isRespawning 플래그 비활성화
    /// </summary>
    private IEnumerator RespawnWhenClear(float _initialDelay)
    {
        isRespawning = true;

        if (_initialDelay > 0f)
            yield return new WaitForSeconds(_initialDelay);

        // 플레이어가 비켜날 때까지 반복 체크
        while (IsPlayerOverlappingRespawnArea())
            yield return new WaitForSeconds(respawnCheckInterval);

        // 플레이어 이동 완료 후 추가 안전 지연
        if (respawnAfterClearDelay > 0f)
            yield return new WaitForSeconds(respawnAfterClearDelay);

        ResetPlatformImmediate();
        isRespawning = false;
    }

    /// <summary>
    /// 리스폰 예정 위치에 플레이어가 겹쳐있는지 확인
    /// 수도코드:
    /// 1. 원본 위치에서의 예정 리스폰 박스 영역 계산
    /// 2. playerLayer 기반 OverlapBox 체크 (빠름)
    /// 3. detectByTag가 true면 추가로 태그 기반 OverlapBoxAll 체크
    /// 4. 플레이어 발견 시 true, 없으면 false 반환
    /// </summary>
    private bool IsPlayerOverlappingRespawnArea()
    {
        if (platformCollider == null)
            return false;

        GetPlannedRespawnBox(out Vector2 _center, out Vector2 _size, out float _angleDeg);

        // 레이어 기반 빠른 체크
        Collider2D _hit = Physics2D.OverlapBox(_center, _size, _angleDeg, playerLayer);
        if (_hit != null)
            return true;

        // 태그 기반 추가 체크
        if (detectByTag)
        {
            var _hits = Physics2D.OverlapBoxAll(_center, _size, _angleDeg);
            for (int _i = 0; _i < _hits.Length; _i++)
            {
                if (_hits[_i].CompareTag(playerTag))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 원본 위치에서의 리스폰 영역 박스 정보 계산
    /// 수도코드:
    /// 1. 현재 스케일 기반 월드 콜라이더 오프셋 계산
    /// 2. 원본 위치에 오프셋 더하기
    /// 3. 스케일 반영한 박스 크기 계산
    /// 4. 원본 각도 적용
    /// </summary>
    private void GetPlannedRespawnBox(out Vector2 _center, out Vector2 _size, out float _angleDeg)
    {
        Vector3 _lossy = transform.lossyScale;

        Vector2 _worldOffset = new Vector2(
            platformCollider.offset.x * _lossy.x,
            platformCollider.offset.y * _lossy.y
        );

        _center = (Vector2)originalWorldPos + _worldOffset;

        _size = new Vector2(
            platformCollider.size.x * Mathf.Abs(_lossy.x),
            platformCollider.size.y * Mathf.Abs(_lossy.y)
        );

        _angleDeg = originalAngleZ;
    }

    /// <summary>
    /// 플랫폼 완전 초기화: 위치, 회전, 물리, 콜라이더, Visual, 옵션 모두 복구
    /// 수도코드:
    /// 1. 위치와 회전을 원본 상태로 설정
    /// 2. Rigidbody를 Kinematic으로 복구, 모든 움직임 정지
    /// 3. platformCollider 활성화
    /// 4. damageColliderObject 비활성화
    /// 5. Visual 원상복구
    /// 6. SpriteRenderer 알파 복구 (1.0)
    /// 7. 모든 옵션을 초기값으로 복구
    /// 8. 모든 상태 플래그 초기화
    /// </summary>
    private void ResetPlatformImmediate()
    {
        // 위치와 회전 원복
        transform.position = originalWorldPos;
        transform.rotation = Quaternion.Euler(0, 0, originalAngleZ);

        // Rigidbody 원복
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0;
        rb.freezeRotation = true;

        // 충돌체 관리
        if (platformCollider)
            platformCollider.enabled = true;

        if (damageColliderObject != null)
            damageColliderObject.SetActive(false);

        // Visual 원복
        ResetVisualOnly();

        // 투명도 원복
        if (spriteRenderer != null)
        {
            var _color = spriteRenderer.color;
            _color.a = 1f;
            spriteRenderer.color = _color;
        }

        // 옵션 초기값 복구
        enableCollisionTrigger = originalEnableCollisionTrigger;
        detectByTag = originalDetectByTag;
        fallMode = originalFallMode;
        enableGroundDetection = originalEnableGroundDetection;
        enableDamageColliderOnFall = originalEnableDamageColliderOnFall;
        respawn = originalRespawn;

        // 통합 bool 값을 각 모드별 내부 변수에 다시 적용
        enableDamageColliderOnSettleMode = enableDamageColliderOnFall;
        enableDamageColliderOnFreeFall = enableDamageColliderOnFall;

        // 상태 플래그 초기화
        isTriggered = false;
        isFalling = false;
        isPlayerDetectionDisabled = false;
    }

    /// <summary>
    /// Visual 트랜스폼만 원상복구
    /// 수도코드:
    /// 1. 로컬 위치를 원본으로 설정 (흔들림 효과 제거)
    /// 2. 로컬 회전을 원본으로 설정 (회전 효과 제거)
    /// </summary>
    private void ResetVisualOnly()
    {
        visualRoot.localPosition = visualOriginalLocalPos;
        visualRoot.localRotation = visualOriginalLocalRot;
    }

    /// <summary>
    /// 화면 밖으로 나갔을 때 호출
    /// 수도코드:
    /// 1. 낙하 중이 아니면 무시
    /// 2. 낙하 중이면 데미지 콜라이더 비활성화 (안전장치)
    /// </summary>
    void OnBecameInvisible()
    {
        if (!isFalling)
            return;

        // 화면 밖에서도 데미지 콜라이더 확실히 비활성화
        if (damageColliderObject != null)
            damageColliderObject.SetActive(false);
    }
}