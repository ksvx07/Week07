using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Data.Common;

[RequireComponent(typeof(DistanceJoint2D))]
[RequireComponent(typeof(LineRenderer))]
public class NewTriangle : MonoBehaviour, IPlayerController
{
    private PlayerInput inputActions;
    private Vector2 moveInput;
    private Rigidbody2D rb;
    private Collider2D col;

    // --- (기존 Inspector 변수들은 동일) ---
    [Header("Move")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float speedAcceleration = 5f;
    [SerializeField] private float SpeedDeceleration = 5f;
    [SerializeField] private float TurningSpeedAcceleration = 80f;

    [Header("Jump / Gravity")]
    [SerializeField] private float maxJumpSpeed = 5f;
    [SerializeField] private float jumpDcceleration = 5f;
    [SerializeField] private float swingJumpDcceleration = 2f;
    [SerializeField] private float maxGravity = 5f;
    [SerializeField] private float gravityAcceleration = 5f;
    [SerializeField] private float maxDownSpeed = 5f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;


    [Header("Wall Jump")]
    [SerializeField] private float wallCheckDistance = 0.4f;
    [SerializeField] private float wallJumpXSpeed = 5f;
    [SerializeField] private float wallJumpYSpeed = 5f;
    [SerializeField] private float wallSlideMaxSpeed = 5f;
    [SerializeField] private float centerOffset = 0.3f;


    [Header("AirTimeMultiplier")]
    [SerializeField] private float airAccelMulti = 0.65f;
    [SerializeField] private float airDecelMulti = 0.65f;

    [Header("Rope Swing")]
    [SerializeField] private bool toggleSwing = true;
    [SerializeField] private GameObject swingPointIndicator;
    [SerializeField] private LayerMask swingableLayer;
    [SerializeField] private float swingTangentialForce = 5f;
    [SerializeField] private float swingBrakeForceMultiplier = 2.0f;
    [SerializeField] private float swingGravityScale = 1.0f;
    [SerializeField] private Material ropeMaterial;
    [SerializeField] private float ropeWidth = 0.1f;
    [SerializeField] private float swingRayDistance = 7f;
    [SerializeField] private Transform spriteChild;
    [SerializeField] private float rotationResetSpeed = 20f;
    [SerializeField] private float swingRotationSpeed = 15f;
    [Header("Ground Dash")]
    [SerializeField] private float groundDashCheckDistance = 1.0f;
    [SerializeField] private float groundDashForce = 10f;
    [SerializeField] private float groundDashDuration = 0.2f;


    [Header("Rope Visuals")] // [새로 추가]
    [SerializeField] private int ropeSegments = 20; // 로프를 그릴 포인트 수 (부드러움)
    [SerializeField] private float ropeSagMultiplier = 0.5f; // 로프가 처지는 정도
    [SerializeField] private float indicatorStabilityThreshold = 0.5f; // 인디케이터 위치 변경 최소 거리
    private string cantSwingTag = "CantSwing";


    private LayerMask wallLayer;

    private float currentGravity;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private float currentJumpDcceleration;

    public bool IsGrounded { get; private set; }
    public bool IsJumping { get; private set; }
    private bool isTouchingWallRight;
    private bool isTouchingWallLeft;
    private bool isFastFalling;
    private int facingDirection = 1;
    private Vector3 originalScale;
    private bool isSwinging = false;
    private bool isDashingToSwing = false;

    private DistanceJoint2D swingJoint;
    private LineRenderer lineRenderer;
    private Vector2? cachedSwingPoint = null; // 캐시된 스윙 포인트
    [Header("Game Log 용")]
    [SerializeField] PlayerDataLog playerDataLog;

    private void Awake()
    {
        inputActions = new PlayerInput();
        col = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        currentGravity = jumpDcceleration;
        currentJumpDcceleration = jumpDcceleration;
        wallLayer = LayerMask.GetMask("Ground");

        originalScale = transform.localScale;

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 0f;

        swingJoint = GetComponent<DistanceJoint2D>();
        lineRenderer = GetComponent<LineRenderer>();

        swingJoint.enabled = false;
        swingJoint.autoConfigureDistance = false;
        swingJoint.maxDistanceOnly = true;

        ConfigureLineRenderer();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Jump.started += OnJump;
        inputActions.Player.Jump.canceled += OffJump;
        inputActions.Player.Dash.performed += OnSwing;
        inputActions.Player.Dash.canceled += OffSwing;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Jump.started -= OnJump;
        inputActions.Player.Jump.canceled -= OffJump;
        inputActions.Player.Dash.performed -= OnSwing;
        inputActions.Player.Dash.canceled -= OffSwing;
        inputActions.Player.Disable();
        jumpBufferCounter = -1;
        moveInput = Vector2.zero;
        IsGrounded = false;

        StopSwing();

        if (spriteChild != null)
        {
            spriteChild.localRotation = Quaternion.identity;
        }
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        if (PlayerManager.Instance.IsHold) return;
        moveInput = ctx.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        if (isSwinging)
        {
            StopSwing();

            if (rb.linearVelocity.y > 0)
            {
                IsJumping = true;
                currentJumpDcceleration = swingJumpDcceleration;
            }
            else
            {
                IsJumping = false;
            }
        }
        else
        {
            jumpBufferCounter = jumpBufferTime;
            isFastFalling = false;
        }
    }

    private void OffJump(InputAction.CallbackContext ctx)
    {
        FastFall();
    }

    private void OnSwing(InputAction.CallbackContext ctx)
    {
        if (!toggleSwing)
        {
            if (isSwinging)
            {
                StopSwing();

                if (rb.linearVelocity.y > 0)
                {
                    IsJumping = true;
                    currentJumpDcceleration = swingJumpDcceleration;
                }
                else
                {
                    IsJumping = false;
                }
            }
            else
            {
                StartSwing();
            }
        }
        else
        {
            if (!isSwinging)
                StartSwing();
        }

    }

    private void OffSwing(InputAction.CallbackContext ctx)
    {
        if (!toggleSwing) return;
        if (isSwinging)
        {
            StopSwing();
            if (rb.linearVelocity.y > 0)
            {
                IsJumping = true;
                currentJumpDcceleration = swingJumpDcceleration;
            }
        }
    }

    /// <summary>
    /// [수정됨] DrawRope() 제거, HandleGroundedSwingState() 유지
    /// </summary>
    private void Update()
    {
        TimeCounters();
        // DrawRope(); // LateUpdate에서 처리
        HandleRotation();
        UpdateSwingPointIndicator();

        if (swingHitCollider != null && isSwinging)
        {
            if (!swingHitCollider.enabled)
            {
                StopSwing();
            }
        }
    }
    private void UpdateSwingPointIndicator()
    {
        if (swingPointIndicator == null) return;

        // 스윙 중일 때는 인디케이터 숨김
        if (isSwinging)
        {
            if (swingPointIndicator.activeSelf)
                swingPointIndicator.SetActive(false);
            cachedSwingPoint = null; // 캐시 초기화
            return;
        }

        // 스윙 포인트 찾기
        RaycastHit2D hit = FindBestSwingPoint();

        if (hit.collider != null)
        {
            Vector2 newSwingPoint = hit.point;

            // 캐시된 포인트가 없거나, 새 포인트가 충분히 멀리 있을 때만 업데이트
            if (cachedSwingPoint == null ||
                Vector2.Distance(cachedSwingPoint.Value, newSwingPoint) > indicatorStabilityThreshold)
            {
                cachedSwingPoint = newSwingPoint;
            }
            // 스윙 포인트가 있으면 해당 위치에 표시
            if (!swingPointIndicator.activeSelf)
                swingPointIndicator.SetActive(true);

            swingPointIndicator.transform.position = cachedSwingPoint.Value;

        }
        else
        {
            // 스윙 포인트가 없으면 숨김
            if (swingPointIndicator.activeSelf)
                swingPointIndicator.SetActive(false);
            cachedSwingPoint = null; // 캐시 초기화
        }
    }

    /// <summary>
    /// [새로 추가] 물리 계산이 끝난 후 로프 비주얼을 갱신합니다.
    /// </summary>
    private void LateUpdate()
    {
        UpdateRopeVisuals();
    }

    private void TimeCounters()
    {
        jumpBufferCounter -= Time.deltaTime;
        if (jumpBufferCounter < 0)
            isFastFalling = false;
        if (IsGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            dashCount = maxDashCount;
        }
        else
            coyoteTimeCounter -= Time.deltaTime;

    }

    private void FixedUpdate()
    {
        WallCheck();
        DetectGround();

        if (!isSwinging)
        {
            if (rb.gravityScale != 0f) rb.gravityScale = 0f;
            Jump();
            // WallJump();
            ApplyGravity();
            Move();
            return;
        }

        if (isDashingToSwing)
        {
            return;
        }

        if (rb.gravityScale != swingGravityScale)
            rb.gravityScale = swingGravityScale;

        SwingMovement();
    }

    // --- (CornerCorrection, Move, DetectGround... 함수들은 동일합니다) ---
    // (이전 코드와 동일한 부분은 생략)


    private void Move()
    {
        float accel = speedAcceleration;
        float decel = SpeedDeceleration;
        float turnAccel = TurningSpeedAcceleration;
        if (!IsGrounded)
        {
            accel *= airAccelMulti;
            decel *= airDecelMulti;
            turnAccel *= airAccelMulti;
        }

        if (moveInput.x > 0)
        {
            facingDirection = 1;
            transform.localScale = originalScale;
        }
        else if (moveInput.x < 0)
        {
            facingDirection = -1;
            Vector3 flippedScale = originalScale;
            flippedScale.x = -originalScale.x;
            transform.localScale = flippedScale;
        }


        if (moveInput.x != 0)
        {
            if (Mathf.Sign(rb.linearVelocity.x) == Mathf.Sign(moveInput.x))
            {
                if (Mathf.Abs(rb.linearVelocity.x) <= maxSpeed + 0.01f)
                {
                    if (Mathf.Abs(rb.linearVelocity.x + accel * moveInput.x * Time.fixedDeltaTime) >= maxSpeed)
                    {
                        rb.linearVelocityX = maxSpeed * Mathf.Sign(moveInput.x);
                    }
                    else
                        rb.linearVelocityX += accel * moveInput.x * Time.fixedDeltaTime;
                }
                else
                {
                    rb.linearVelocityX -= decel * Mathf.Sign(rb.linearVelocity.x) * Time.fixedDeltaTime;
                }
            }
            else
            {
                rb.linearVelocityX += turnAccel * moveInput.x * Time.fixedDeltaTime;
            }

        }
        else
        {
            rb.linearVelocityX -= decel * Mathf.Sign(rb.linearVelocity.x) * Time.fixedDeltaTime;
            if (Mathf.Sign(rb.linearVelocity.x) != Mathf.Sign(rb.linearVelocity.x - decel * Mathf.Sign(rb.linearVelocity.x) * Time.fixedDeltaTime))
            {
                rb.linearVelocityX = 0;
            }
        }

    }

    // private void DetectGround()
    // {
    //     Bounds bounds = col.bounds;
    //     float extraHeight = 0.05f;

    //     RaycastHit2D hit = Physics2D.BoxCast(bounds.center, bounds.size, 0f, Vector2.down,
    //         extraHeight, wallLayer);

    //     IsGrounded = hit.collider != null;


    //     if (IsJumping && rb.linearVelocity.y <= 0)
    //     {
    //         IsJumping = false;
    //         currentGravity = jumpDcceleration;
    //     }
    // }

    private void DetectGround()
    {
        Bounds bounds = col.bounds;
        float extraHeight = 0.05f;

        // 좌우 코너 위치 계산
        Vector2 leftOrigin = new Vector2(bounds.min.x + 0.05f, bounds.min.y);
        Vector2 rightOrigin = new Vector2(bounds.max.x - 0.05f, bounds.min.y);

        // 아래로 레이캐스트
        RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.down, extraHeight, wallLayer);
        RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.down, extraHeight, wallLayer);

        // 디버그용 시각화
        Debug.DrawRay(leftOrigin, Vector2.down * extraHeight, Color.green);
        Debug.DrawRay(rightOrigin, Vector2.down * extraHeight, Color.green);

        bool grounded = (leftHit.collider != null || rightHit.collider != null);

        // 벽 슬라이드 상태일 땐 false 처리
        bool isWallSliding = (isTouchingWallRight || isTouchingWallLeft) && rb.linearVelocity.y < 0f;
        if (grounded && isWallSliding)
            IsGrounded = false;
        else
            IsGrounded = grounded;

        // 점프 중 상태 해제
        if (IsJumping && rb.linearVelocity.y <= 0)
        {
            IsJumping = false;
            currentGravity = jumpDcceleration;
        }
    }

    private void ApplyGravity()
    {
        float newY;
        if (IsJumping)
        {
            newY = rb.linearVelocity.y - currentJumpDcceleration * Time.fixedDeltaTime;
        }
        else
        {
            if (currentGravity < maxGravity)
                currentGravity += gravityAcceleration * Time.fixedDeltaTime;
            else
                currentGravity = maxGravity;

            newY = rb.linearVelocity.y - currentGravity * Time.fixedDeltaTime;
        }

        if (isTouchingWallRight || isTouchingWallLeft)
            if (newY < -wallSlideMaxSpeed)
                newY = -wallSlideMaxSpeed;

        newY = Mathf.Clamp(newY, -maxDownSpeed, maxJumpSpeed);

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);
    }

    private void Jump()
    {
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            // Debug.Log("Jump!");
            IsJumping = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxJumpSpeed);
            currentJumpDcceleration = jumpDcceleration;
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
            if (isFastFalling)
                IsJumping = false;
        }
    }

    private void FastFall()
    {
        if (IsJumping)
        {
            IsJumping = false;
        }

        if (jumpBufferCounter > 0)
            isFastFalling = true;

    }

    // --- (WallCheck, WallJump, OnEnableSetVelocity, ConfigureLineRenderer, StartSwing, GroundDashSwing, AttachSwingJoint, StopSwing... 함수들은 모두 동일합니다) ---
    // (이전 코드와 동일한 부분은 생략)
    private void WallCheck()
    {
        Vector2 origin = transform.position - new Vector3(0f, centerOffset, 0f);
        RaycastHit2D hitWallRight = new RaycastHit2D();
        RaycastHit2D hitWallLeft = new RaycastHit2D();
        hitWallRight = Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, wallLayer);
        Debug.DrawRay(origin, Vector2.right * wallCheckDistance, Color.red);
        hitWallLeft = Physics2D.Raycast(origin, Vector2.left, wallCheckDistance, wallLayer);
        Debug.DrawRay(origin, Vector2.left * wallCheckDistance, Color.red);


        isTouchingWallRight = hitWallRight.collider != null;
        isTouchingWallLeft = hitWallLeft.collider != null;

    }

    private void WallJump()
    {
        if ((isTouchingWallRight || isTouchingWallLeft) && jumpBufferCounter > 0 && !IsGrounded)
        {
            int wallJumpDir;
            if (isTouchingWallRight)
                wallJumpDir = -1;
            else
                wallJumpDir = 1;

            IsJumping = true;
            rb.linearVelocity = new Vector2(wallJumpXSpeed * wallJumpDir, wallJumpYSpeed);
            // Debug.Log("Wall Jump");
        }
    }
    private int maxDashCount = 1;
    public int dashCount { get; set; }
    public void OnEnableSetVelocity(float newVelX, float newVelY, int currentDashCount)
    {
        // Debug.Log("Set Velocity Called");
        col = GetComponent<PolygonCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        currentGravity = jumpDcceleration;
        wallLayer = LayerMask.GetMask("Ground");

        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(newVelX, newVelY);
        dashCount = currentDashCount;
    }


    // --- Rope Swing ---

    private void ConfigureLineRenderer()
    {
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth;
        lineRenderer.material = ropeMaterial;
        lineRenderer.sortingLayerName = "Foreground";
        lineRenderer.enabled = false;
    }

    private Collider2D swingHitCollider;

    private void StartSwing()
    {
        if (isDashingToSwing) return;
        swingHitCollider = null;

        // 인디케이터가 활성화되어 있고 캐시된 스윙 포인트가 있으면 그것을 사용
        Vector2 swingPoint = Vector2.zero;
        bool hasValidSwingPoint = false;

        if (swingPointIndicator != null && swingPointIndicator.activeSelf && cachedSwingPoint.HasValue)
        {
            // 인디케이터 위치를 스윙 포인트로 사용
            swingPoint = cachedSwingPoint.Value;

            // 해당 위치에 실제로 스윙 가능한 오브젝트가 있는지 확인
            RaycastHit2D verifyHit = Physics2D.Raycast(transform.position,
                (swingPoint - (Vector2)transform.position).normalized,
                swingRayDistance,
                swingableLayer);

            if (verifyHit.collider != null)
            {
                hasValidSwingPoint = true;

                if (verifyHit.collider.TryGetComponent<BreakablePlatform>(out BreakablePlatform crumbleTile))
                {
                    crumbleTile.TriggerBreak();
                    swingHitCollider = verifyHit.collider;
                }
            }
        }

        // 유효한 스윙 포인트가 없으면 기존 방식으로 찾기
        if (!hasValidSwingPoint)
        {
            RaycastHit2D hit = FindBestSwingPoint();

            if (hit.collider == null) return;

            swingPoint = hit.point;

            if (hit.collider.TryGetComponent<BreakablePlatform>(out BreakablePlatform crumbleTile))
            {
                crumbleTile.TriggerBreak();
                swingHitCollider = hit.collider;
            }
        }

        // 스윙 시작 시 인디케이터 숨김
        if (swingPointIndicator != null && swingPointIndicator.activeSelf)
            swingPointIndicator.SetActive(false);

        playerDataLog.OnPlayerUseAbility();
        isSwinging = true;
        swingJoint.connectedAnchor = swingPoint;
        lineRenderer.enabled = true;

        Bounds bounds = col.bounds;
        RaycastHit2D groundHit = Physics2D.Raycast(transform.position, Vector2.down,
            groundDashCheckDistance, wallLayer);
        Debug.DrawRay(transform.position, Vector2.down * groundDashCheckDistance, Color.blue);

        if (groundHit.collider != null)
        {
            StartCoroutine(GroundDashSwing(swingPoint));
        }
        else
        {
            AttachSwingJoint(swingPoint);
        }
    }

    private IEnumerator GroundDashSwing(Vector2 anchorPoint)
    {
        Debug.Log("Ground Dash Swing");
        isDashingToSwing = true;

        Vector2 dashDir = (anchorPoint - (Vector2)transform.position).normalized;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(dashDir * groundDashForce, ForceMode2D.Impulse);

        rb.gravityScale = swingGravityScale;

        float timer = 0f;
        while (timer < groundDashDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        float currentDistance = Vector2.Distance(transform.position, anchorPoint);
        swingJoint.distance = currentDistance;

        swingJoint.enabled = true;

        isDashingToSwing = false;
    }

    private void AttachSwingJoint(Vector2 anchorPoint)
    {
        float currentDistance = Vector2.Distance(transform.position, anchorPoint);
        swingJoint.distance = currentDistance;

        swingJoint.enabled = true;
        rb.gravityScale = swingGravityScale;
    }

    public void StopSwing()
    {
        isSwinging = false;
        swingJoint.enabled = false;
        lineRenderer.enabled = false;

        if (isDashingToSwing)
        {
            StopAllCoroutines();
            isDashingToSwing = false;
        }
    }

    // [제거됨] private void DrawRope()
    // [제거됨] private void UpdateRopePositions(Vector2 anchorPoint)

    /// <summary>
    /// [새로 추가] 로프 비주얼을 팽팽/느슨 상태에 따라 갱신합니다.
    /// </summary>
    private void UpdateRopeVisuals()
    {
        if (!isSwinging)
        {
            if (lineRenderer.enabled)
                lineRenderer.enabled = false;
            return;
        }

        if (!lineRenderer.enabled)
            lineRenderer.enabled = true;

        Vector2 playerPos = transform.position;
        Vector2 anchorPoint = swingJoint.connectedAnchor;

        float ropeMaxLength = swingJoint.distance;
        float currentDistance = Vector2.Distance(playerPos, anchorPoint);

        // 밧줄이 팽팽할 때 (거의 최대 길이에 근접)
        if (currentDistance >= ropeMaxLength * 0.99f || isDashingToSwing)
        {
            if (lineRenderer.positionCount != 2)
                lineRenderer.positionCount = 2;

            lineRenderer.SetPosition(0, new Vector3(playerPos.x, playerPos.y, 0));
            lineRenderer.SetPosition(1, new Vector3(anchorPoint.x, anchorPoint.y, 0));
        }
        else // 밧줄이 느슨할 때
        {
            if (lineRenderer.positionCount != ropeSegments + 1)
                lineRenderer.positionCount = ropeSegments + 1;

            // 베지어 커브의 중간 제어점(Control Point) 계산
            Vector2 midPoint = (playerPos + anchorPoint) / 2f;
            float slack = ropeMaxLength - currentDistance;
            Vector2 controlPoint = midPoint + (Vector2.down * slack * ropeSagMultiplier);

            // 커브 위의 점들 계산
            for (int i = 0; i <= ropeSegments; i++)
            {
                float t = (float)i / ropeSegments;
                float u = 1 - t;

                // 2차 베지어 커브 공식: (1-t)^2 * P0 + 2(1-t)t * P1 + t^2 * P2
                Vector2 pointOnCurve = (u * u * playerPos) + (2 * u * t * controlPoint) + (t * t * anchorPoint);

                lineRenderer.SetPosition(i, new Vector3(pointOnCurve.x, pointOnCurve.y, 0));
            }
        }
    }

    private RaycastHit2D FindBestSwingPoint()
    {
        Vector2 rayOrigin = transform.position;
        float playerY = rayOrigin.y;

        Vector2 dir45 = GetDirectionVectorFromAngle(45f, facingDirection);
        RaycastHit2D hit45 = Physics2D.Raycast(rayOrigin, dir45, swingRayDistance, swingableLayer);

        if (hit45.collider != null && hit45.point.y > playerY)
        {
            if (hit45.collider.CompareTag(cantSwingTag))
                hit45 = new RaycastHit2D();
            else
                return hit45;

        }

        for (float deltaAngle = 5f; deltaAngle <= 40f; deltaAngle += 5f)
        {
            float angleUp = 45f + deltaAngle;
            Vector2 dirUp = GetDirectionVectorFromAngle(angleUp, facingDirection);
            RaycastHit2D hitUp = Physics2D.Raycast(rayOrigin, dirUp, swingRayDistance, swingableLayer);

            if (hitUp.collider != null && hitUp.point.y > playerY)
            {
                if (hitUp.collider.CompareTag(cantSwingTag))
                    hitUp = new RaycastHit2D();
                else
                    return hitUp;
            }

            float angleDown = 45f - deltaAngle;
            Vector2 dirDown = GetDirectionVectorFromAngle(angleDown, facingDirection);
            RaycastHit2D hitDown = Physics2D.Raycast(rayOrigin, dirDown, swingRayDistance, swingableLayer);

            if (hitDown.collider != null && hitDown.point.y > playerY)
            {
                if (hitDown.collider.CompareTag(cantSwingTag))
                    hitDown = new RaycastHit2D();
                else
                    return hitDown;
            }
        }

        return new RaycastHit2D();
    }

    private void SwingMovement()
    {
        if (moveInput.x > 0)
        {
            facingDirection = 1;
            transform.localScale = originalScale;
        }
        else if (moveInput.x < 0)
        {
            facingDirection = -1;
            Vector3 flippedScale = originalScale;
            flippedScale.x = -originalScale.x;
            transform.localScale = flippedScale;
        }

        if (moveInput.x != 0)
        {
            Vector2 ropeDir = (Vector2)swingJoint.connectedAnchor - rb.position;
            Vector2 tangentDir;

            if (moveInput.x > 0)
            {
                tangentDir = new Vector2(ropeDir.y, -ropeDir.x).normalized;
            }
            else
            {
                tangentDir = new Vector2(-ropeDir.y, ropeDir.x).normalized;
            }

            float forceMultiplier = 1.0f;
            float dot = Vector2.Dot(rb.linearVelocity, tangentDir);

            if (dot < 0)
            {
                forceMultiplier = swingBrakeForceMultiplier;
            }

            rb.AddForce(tangentDir * swingTangentialForce * forceMultiplier, ForceMode2D.Force);
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Vector2 rayOrigin = transform.position;

            Gizmos.color = Color.cyan;
            Vector2 dir45 = GetDirectionVectorFromAngle(45f, facingDirection);
            Gizmos.DrawRay(rayOrigin, dir45 * swingRayDistance);

            Gizmos.color = Color.yellow;
            for (float deltaAngle = 5f; deltaAngle <= 40f; deltaAngle += 5f)
            {
                float angleUp = 45f + deltaAngle;
                Vector2 dirUp = GetDirectionVectorFromAngle(angleUp, facingDirection);
                Gizmos.DrawRay(rayOrigin, dirUp * swingRayDistance);

                float angleDown = 45f - deltaAngle;
                Vector2 dirDown = GetDirectionVectorFromAngle(angleDown, facingDirection);
                Gizmos.DrawRay(rayOrigin, dirDown * swingRayDistance);
            }
        }
    }

    private Vector2 GetDirectionVectorFromAngle(float angleInDegrees, int facingDir)
    {
        float angleInRadians = angleInDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angleInRadians) * facingDir, Mathf.Sin(angleInRadians));
    }

    private void HandleRotation()
    {
        if (spriteChild == null)
        {
            Debug.LogWarning("Sprite Child Transform이 할당되지 않았습니다. 회전 기능이 작동하지 않습니다.");
            return;
        }

        if (isSwinging)
        {
            Vector2 ropeDir = (Vector2)swingJoint.connectedAnchor - rb.position;
            float targetAngle = Mathf.Atan2(ropeDir.y, ropeDir.x) * Mathf.Rad2Deg - 90f;

            Quaternion targetRotation = Quaternion.Euler(0, 0, targetAngle);
            spriteChild.rotation = Quaternion.Slerp(spriteChild.rotation, targetRotation, Time.deltaTime * swingRotationSpeed);
        }
        else
        {
            spriteChild.localRotation = Quaternion.Slerp(spriteChild.localRotation, Quaternion.identity, Time.deltaTime * rotationResetSpeed);
        }
    }
}
