using System.Collections;
using UnityEngine;

public class KirbyController : MonoBehaviour, IPlayerController
{
    // 능력 사용 데이터 로그
    [Header("Game Log 용")]
    [SerializeField] PlayerDataLog playerDataLog;

    #region References
    Rigidbody2D _rb;
    KirbyGroundCheck _groundCheck;
    #endregion

    [Header("Movement Stats")]
    [SerializeField, Range(0f, 20f)][Tooltip("최대 속도")] private float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)][Tooltip("가속도")] private float maxAcceleration = 52f;
    [SerializeField, Range(0f, 100f)][Tooltip("감속도")] private float maxDecceleration = 52f;
    [SerializeField, Range(1f, 100f)][Tooltip("방향 전환 속도")] private float maxTurnSpeed = 80f;
    [SerializeField, Range(0f, 100f)][Tooltip("공중 가속도")] private float maxAirAcceleration;
    [SerializeField, Range(0f, 100f)][Tooltip("공중 감속도")] private float maxAirDeceleration;
    [SerializeField, Range(0f, 100f)][Tooltip("공중 방향 전환 속도")] private float maxAirTurnSpeed = 80f;

    [Header("Turbo Stats")]
    [SerializeField, Range(0f, 20f)][Tooltip("터보 속도")] private float turboSpeed = 20f;
    [SerializeField, Range(0f, 100f)][Tooltip("터보 감속도")] private float turboDecceleration = 52f;

    [Header("Bounce Settings")]
    [Tooltip("X방향 튀어오르는 힘")]
    [SerializeField] private float bounceStrength = 5f;
    [Tooltip("Y방향 튀어오르는 힘")]
    [SerializeField] private float bounceHeight = 10f;
    [Tooltip("튀어오르는 효과가 지속되는 최소 시간")]
    [SerializeField] private float bounceDuration = 0.3f;
    private bool isBouncing = false; // 벽에 튀어오르는 상태
    private bool isFixedBouncing = false; // 튀어오르는 동안 입력 무시 시간

    [Header("Current State")]
    [SerializeField]
    private LayerMask groundLayer;
    public bool onGround;
    public bool pressingKey; // 이동 키를 누르고 있는지 확인
    private bool turboMode;

    #region Private - Speed Caculation Variables
    private Vector2 desiredVelocity; // 원하는 목표 속도
    private float directionX; // 이동 방향: -1(왼쪽), +1(오른쪽)
    private float turboDirectionX; // 터보 모드 이동 방향
    private float maxSpeedChangeAmount; // 한 프레임당 변경 가능한 최대 속도 변화량
    private float acceleration; // 현재 가속도
    private float deceleration; // 현재 감속도
    private float turnSpeed; // 방향 전환 속도
    #endregion

    #region Public - Return Speed Variables
    public float DirectionX
    {
        get { return directionX; }
        set { directionX = value; }
    }
    public float MaxSpeed
    {
        get { return maxSpeed; }
    }
    public bool TurboMode
    {
        get { return turboMode; }
    }
    #endregion

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _groundCheck = GetComponent<KirbyGroundCheck>();
    }

    private void OnDisable()
    {
        InitializedCircle();
    }

    private void Update()
    {
        // 지상에 있으면 대시 횟수 초기화
        if (_groundCheck.GetOnGround())
            dashCount = maxDashCount;

        // 튀어오르는 동안 최소 시간 동안은 입력 무시
        if (isFixedBouncing)
        {
            return;
        }

        // 입력값에 따라 캐릭터 방향 전환
        if (directionX != 0)
        {
            transform.localScale = new Vector3(directionX > 0 ? 1 : -1, 1, 1);
            pressingKey = true;
        }
        else
        {
            pressingKey = false;
        }

        // 튀어오르는 상태에서 입력이 있으면 바운스 상태 해제
        if (isBouncing)
        {
            if (pressingKey) { isBouncing = false; }
            else return;
        }

        // 터보 모드 여부에 따라 목표 속도 설정
    }

    private void FixedUpdate()
    {
        // 튀어오르는 동안 최소 시간 동안은 물리 처리 무시
        if (isFixedBouncing) return;

        onGround = _groundCheck.GetOnGround();

        // 튀어오르는 상태에서 지상에 착지하면 바운스 해제
        if (isBouncing)
        {
            if (onGround) { isBouncing = false; }
            return;
        }


        if (turboMode)
        {
            // 터보 모드: 즉시 속도 변경
            runWithoutAcceleration();
        }
        else
        {
            terboLayRotation = Vector2.down;
            // 일반 모드: 가속도를 적용한 이동
            runWithAcceleration();
        }
    }

    private Vector2 previousVelocity;

    // 벽과 충돌 시 바운스 처리
    private void OnCollisionStay2D(Collision2D collision)
    {
        // 바운스 중이거나 터보 모드가 아니면 무시
        if (isBouncing || !turboMode) return;

        if (groundLayer != 0)
        {
            // 모든 충돌 지점을 확인하여 벽인지 판단
            foreach (ContactPoint2D contact in collision.contacts)
            {
                Vector2 normal = contact.normal;
                float angle = Vector2.Angle(terboLayRotation, normal) - 90f;

                if (angle < maxCollisionAngle)
                {
                    turboMode = false;
                    StartCoroutine(Bounce(collision));
                    return;
                }
            }
        }
    }

    // 커비 상태 초기화
    private void InitializedCircle()
    {
        turboMode = false;
        isFixedBouncing = false;
        isBouncing = false;
    }

    // 가속도를 적용한 이동 (일반 모드)
    private void runWithAcceleration()
    {
        _rb.gravityScale = fixeNormaldGravity;

        // 지상/공중 여부에 따라 가속도 설정
        acceleration = onGround ? maxAcceleration : maxAirAcceleration;
        deceleration = onGround ? maxDecceleration : maxAirDeceleration;
        turnSpeed = onGround ? maxTurnSpeed : maxAirTurnSpeed;

        // 이동 키를 누르고 있을 때
        if (pressingKey)
        {
            // 같은 방향으로 이동 중
            if (Mathf.Sign(directionX) == Mathf.Sign(_rb.linearVelocity.x))
            {
                // 최대 속도 미만이면 가속
                if (Mathf.Abs(_rb.linearVelocity.x) < maxSpeed)
                {
                    if (Mathf.Abs(_rb.linearVelocity.x + acceleration * directionX * Time.fixedDeltaTime) >= maxSpeed)
                    {
                        _rb.linearVelocityX = maxSpeed * Mathf.Sign(directionX);
                    }
                    else
                        _rb.linearVelocityX += acceleration * directionX * Time.deltaTime;
                }
                else
                {
                    // 최대 속도 초과 시 감속
                    _rb.linearVelocityX -= deceleration * Mathf.Sign(_rb.linearVelocity.x) * Time.deltaTime;
                }
            }
            else
            {
                // 반대 방향으로 전환 시 턴 속도 적용
                _rb.linearVelocityX += turnSpeed * directionX * Time.deltaTime;
            }
        }
        else
        {
            // 키를 떼면 감속
            _rb.linearVelocityX -= deceleration * Mathf.Sign(_rb.linearVelocity.x) * Time.deltaTime;

            // 속도가 0을 넘어가면 정지
            if (Mathf.Sign(_rb.linearVelocity.x) != Mathf.Sign(_rb.linearVelocity.x - deceleration * Mathf.Sign(_rb.linearVelocity.x) * Time.fixedDeltaTime))
            {
                _rb.linearVelocityX = 0;
            }
        }
    }

    private Vector2 terboLayRotation = Vector2.down;
    [SerializeField] private float groundCheckDistance = 1f;
    [SerializeField] private float rotationReturnSpeed = 5f; // 회전 복귀 속도
    [SerializeField] private float terboForce = 60f;
    [SerializeField] private float maxCollisionAngle = 30.0f;
    [SerializeField] private float fixeNormaldGravity = 4.0f;
    private float fixeterbodGravity = 0f;
    // 즉시 속도 변경 (터보 모드)
    private void runWithoutAcceleration()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, terboLayRotation, groundCheckDistance, groundLayer);
        Debug.DrawRay(transform.position, terboLayRotation * groundCheckDistance, Color.red);
        Vector2 moveDirection;
        if (hit.collider != null)
        {
            // if (_rb.linearVelocity.magnitude > turboSpeed / 2f)
            // {
            _rb.gravityScale = fixeterbodGravity;
            _rb.AddForce(9.81f * fixeNormaldGravity * terboLayRotation * _rb.mass, ForceMode2D.Force);
            // }
            // else
            // {
            // _rb.gravityScale = fixeNormaldGravity;
            // }
            terboLayRotation = -hit.normal;
            moveDirection = new Vector2(-terboLayRotation.y, terboLayRotation.x) * Mathf.Sign(transform.localScale.x);
        }
        else
        {
            _rb.gravityScale = fixeNormaldGravity;
            terboLayRotation = Vector2.Lerp(terboLayRotation, Vector2.down, rotationReturnSpeed * Time.fixedDeltaTime);
            moveDirection = new Vector2(Mathf.Sign(transform.localScale.x), 0);
        }
        _rb.AddForce(terboForce * moveDirection * _rb.mass, ForceMode2D.Force);

        // 속도 제한
        if (_rb.linearVelocity.magnitude > turboSpeed)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * turboSpeed;
        }
    }

    // 벽 충돌 시 바운스 효과
    private IEnumerator Bounce(Collision2D collision)
    {
        isFixedBouncing = true;
        isBouncing = true;


        isFixedBouncing = true;
        isBouncing = true;
        Vector2 fixedBounceVelocity = new Vector2(
            -transform.localScale.x * bounceStrength,
            bounceHeight
        );

        _rb.linearVelocity = fixedBounceVelocity;

        yield return new WaitForSeconds(bounceDuration);
        isFixedBouncing = false;

    }

    #region Public - PlayerInput
    // 이동 입력 처리
    public void OnMoveInput(Vector2 movementInput)
    {
        directionX = movementInput.x;
    }

    // 터보 모드 토글
    public void OnTurboModePressed()
    {
        // 바운스 중에는 터보 모드 변경 불가
        if (isBouncing) return;

        if (turboMode == false)
        {
            // 터보 활성화 시 능력 사용 로그
            playerDataLog.OnPlayerUseAbility();
        }
        turboMode = !turboMode;
    }

    private int maxDashCount = 1;
    public int dashCount { get; set; }

    // 플레이어 상태 전환 시 속도 설정
    public void OnEnableSetVelocity(float newVelX, float newVelY, int currentDashCount)
    {
        _rb = GetComponent<Rigidbody2D>();
        _groundCheck = GetComponent<KirbyGroundCheck>();
        _rb.linearVelocity = new Vector2(newVelX, newVelY);
        dashCount = currentDashCount;
    }
    #endregion
}