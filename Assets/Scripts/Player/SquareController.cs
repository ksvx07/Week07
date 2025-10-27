using UnityEngine;
using UnityEngine.InputSystem;


public class SquareController : MonoBehaviour, IPlayerController
{
    private PlayerInput inputActions;
    private Vector2 moveInput;
    private Rigidbody2D rb;
    private BoxCollider2D col;
    private SpriteRenderer spriteRenderer;

    // Inspector ???? ???? ??????
    [Header("Move")]
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float speedAcceleration = 5f;
    [SerializeField] private float SpeedDeceleration = 5f;
    [SerializeField] private float TurningSpeedAcceleration = 80f;
    [SerializeField] private bool canMove = true;

    [Header("Jump / Gravity")]
    [SerializeField] private float maxJumpSpeed = 5f;
    [SerializeField] private float jumpDcceleration = 5f;
    [SerializeField] private float maxGravity = 5f;
    [SerializeField] private float gravityAcceleration = 5f;
    [SerializeField] private float maxDownSpeed = 5f;
    [SerializeField] private float coyoteTime = 0.1f;       // ????? ??? ????
    [SerializeField] private float jumpBufferTime = 0.1f;   // ???? ???? ????
    [SerializeField] private float cornerRayPosX = 0.3f;
    [SerializeField] private float cornerRayOffsetX = 0.1f;
    [SerializeField] private float cornerRayLength = 0.1f;


    [Header("Wall Jump")]
    [SerializeField] private float wallCheckDistance = 0.4f;
    [SerializeField] private float wallJumpXSpeed = 5f;
    [SerializeField] private float wallJumpYSpeed = 5f;
    [SerializeField] private float wallSlideMaxSpeed = 5f;
    [SerializeField] private float wallDetachTime = 0.2f; // 벽에서 떨어지기 위한 최소 입력 시간
    private float wallDetachCounter = 0f; // 벽 떨어지기 타이머
    private bool isWallGrabbing = false; // 벽을 잡고 있는 상태

    // [Header("Wall Jump Options")]
    // [SerializeField] private float wallJumpStaggerDuration = 0.15f;
    // [SerializeField] private float wallJumpCurveDuration = 0.2f;      // 곡선으로 속도변화하는 시간
    // [SerializeField] private AnimationCurve wallJumpSpeedCurveX = AnimationCurve.Linear(0, 1, 1, 0);  // X축 커브
    // [SerializeField] private AnimationCurve wallJumpSpeedCurveY = AnimationCurve.EaseInOut(0, 1, 1, 0);  // Y축 커브

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 5f;
    [SerializeField] private float dashTime = 0.5f;
    [SerializeField] private float dashCooldown = 0.1f;
    [SerializeField] private float maxSpeedAfterDashX = 5f;
    [SerializeField] private float maxSpeedAfterDashUp = 5f;
    [SerializeField] private int maxDashCount = 1;
    [SerializeField] private GameObject afterImagePrefab; // 잔상 프리팹
    [SerializeField] private float afterImageLifetime = 0.3f; // 잔상 지속 시간
    [SerializeField] private float afterImageSpawnRate = 0.05f; // 잔상 생성 간격
    private float afterImageTimer; // 잔상 생성 타이머
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color noDashColor = Color.white;


    [Header("AirTimeMultiplier")]
    [SerializeField] private float airAccelMulti = 0.65f;
    [SerializeField] private float airDecelMulti = 0.65f;

    private LayerMask wallLayer;

    private float currentGravity;
    private float coyoteTimeCounter; // ???? ???? ?? ???? ???? ???? ?��?
    private float jumpBufferCounter; // ???? ??? ???? ?��?
    private float dashTimeCounter;
    private float dashCooldownCounter;
    // ???? ????
    public bool IsGrounded { get; private set; }
    public bool IsJumping { get; private set; }
    private bool isTouchingWallRight;
    private bool isTouchingWallLeft;
    public bool isDashing;
    public int dashCount { get; set; }
    private bool isFastFalling;
    private int facingDirection = 1; // 1: ?��른쪽, -1: ?���?
    private Vector3 originalScale; // ?���? ?���? ????��

    // // ========== 벽점프 발딛움 상태 변수 ==========
    // private bool isWallJumping = false;
    [SerializeField] private float walljumpTime = 0.1f;
    private float walljumpTimerCounter = 0f;
    [SerializeField] private float wallJumpAccelMulti = 0.1f;
    [SerializeField] private float wallJumpDecelMulti = 0.1f;
    // private float wallJumpElapsedTime = 0f;
    // private float wallJumpInitialVelX = 0f;
    // ============================================
    [Header("Game Log 용")]
    [SerializeField] PlayerDataLog playerDataLog;
    private void Awake()
    {
        inputActions = new PlayerInput();
        col = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentGravity = jumpDcceleration;
        wallLayer = LayerMask.GetMask("Ground");
        dashCount = maxDashCount;
        ChangeColor();
        // ?���? ?���? ????��
        originalScale = transform.localScale;

        // ?���? ?���? ????��
        originalScale = transform.localScale;

        // Rigidbody ????
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 0f; // ????? ???? ???
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Jump.started += OnJump;
        inputActions.Player.Jump.canceled += OffJump;
        inputActions.Player.Dash.performed += OnDash;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Jump.started -= OnJump;
        inputActions.Player.Jump.canceled -= OffJump;
        inputActions.Player.Dash.performed -= OnDash;
        inputActions.Player.Disable();
        moveInput = Vector2.zero;
        jumpBufferCounter = -1;
        IsGrounded = false;
        rb.excludeLayers = 0;
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        if (PlayerManager.Instance.IsHold) return;
        moveInput = ctx.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        jumpBufferCounter = jumpBufferTime;
        isFastFalling = false;
    }

    private void OffJump(InputAction.CallbackContext ctx)
    {
        FastFall();
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
        Dash();
    }

    private void Update()
    {
        TimeCounters();
        CheckWallGrab();
    }

    // 벽 잡기 상태 체크
    private void CheckWallGrab()
    {
        // 벽을 잡고 있는 상태 확인
        if (!IsGrounded && (isTouchingWallRight || isTouchingWallLeft))
        {
            isWallGrabbing = true;

            // 반대 방향 입력 확인
            bool pressingAwayFromWall = false;
            if (isTouchingWallRight && moveInput.x < -0.1f)
                pressingAwayFromWall = true;
            else if (isTouchingWallLeft && moveInput.x > 0.1f)
                pressingAwayFromWall = true;

            if (pressingAwayFromWall)
            {
                wallDetachCounter += Time.deltaTime;
                if (wallDetachCounter >= wallDetachTime)
                {
                    DetachFromWall();
                }
            }
            else
            {
                wallDetachCounter = 0f;
            }
        }
        else
        {
            isWallGrabbing = false;
            wallDetachCounter = 0f;
        }
    }
    // 벽에서 떨어지기
    private void DetachFromWall()
    {
        isWallGrabbing = false;
        wallDetachCounter = 0f;

        // 벽에서 살짝 밀어내기 (선택사항)
        float pushForce = 2f;
        if (isTouchingWallRight)
            rb.linearVelocity = new Vector2(-pushForce, rb.linearVelocity.y);
        else if (isTouchingWallLeft)
            rb.linearVelocity = new Vector2(pushForce, rb.linearVelocity.y);
    }

    // ?��? ??????
    private void TimeCounters()
    {
        // ???? ???? (????) & ????? ???
        jumpBufferCounter -= Time.deltaTime;
        if (jumpBufferCounter < 0)
            isFastFalling = false;
        if (IsGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            if (!isDashing)
            {
                dashCount = maxDashCount;
                ChangeColor();
            }

        }
        else
            coyoteTimeCounter -= Time.deltaTime;

        // ???? ?��? ?????, ??? ?????? Damping ??
        if (isDashing)
        {
            dashTimeCounter -= Time.deltaTime;

            if (dashTimeCounter < 0)
            {
                isDashing = false;
                dampAfterDash();
            }

            // 잔상 효과 생성
            afterImageTimer -= Time.deltaTime;
            if (afterImageTimer <= 0)
            {
                CreateAfterImage();
                afterImageTimer = afterImageSpawnRate;
            }
        }
        dashCooldownCounter -= Time.deltaTime;
        walljumpTimerCounter -= Time.deltaTime;
        if (walljumpTimerCounter < 0)
        {
            // isWallJumping = false;
            walljumpTimerCounter = 0f;
        }
    }

    private void FixedUpdate()
    {
        WallCheck();
        DetectGround();
        // UpdateWallJumpState(); // 벽점프 지속시간 중 관리용
        if (!isDashing)
        {
            WallJump();
            Jump();
            CornerCorrection();
            ApplyGravity();
            if (!isWallGrabbing)
                Move();
        }


        // Debug.Log($"x: {rb.linearVelocity.x:F2}, y: {rb.linearVelocity.y:F2}");
    }




    private void CornerCorrection()
    {
        RaycastHit2D CornerHitRight = Physics2D.Raycast(transform.position + new Vector3(cornerRayPosX, 0, 0), Vector2.up, cornerRayLength, wallLayer);
        RaycastHit2D CornerHitRightOffset = Physics2D.Raycast(transform.position + new Vector3(cornerRayPosX + cornerRayOffsetX, 0, 0), Vector2.up, cornerRayLength, wallLayer);
        RaycastHit2D CornerHitLeft = Physics2D.Raycast(transform.position + new Vector3(-cornerRayPosX, 0, 0), Vector2.up, cornerRayLength, wallLayer);
        RaycastHit2D CornerHitLeftOffset = Physics2D.Raycast(transform.position + new Vector3(-cornerRayPosX - cornerRayOffsetX, 0, 0), Vector2.up, cornerRayLength, wallLayer);
        Debug.DrawRay(transform.position + new Vector3(cornerRayPosX, 0, 0), Vector2.up * cornerRayLength, Color.red);
        Debug.DrawRay(transform.position + new Vector3(cornerRayPosX + cornerRayOffsetX, 0, 0), Vector2.up * cornerRayLength, Color.red);
        Debug.DrawRay(transform.position + new Vector3(-cornerRayPosX, 0, 0), Vector2.up * cornerRayLength, Color.red);
        Debug.DrawRay(transform.position + new Vector3(-cornerRayPosX - cornerRayOffsetX, 0, 0), Vector2.up * cornerRayLength, Color.red);

        if (!CornerHitRight && CornerHitRightOffset && moveInput.x <= 0)
        {
            rb.MovePosition(rb.position + new Vector2(-cornerRayPosX + cornerRayOffsetX, 0));
        }
        else if (!CornerHitLeft && CornerHitLeftOffset && moveInput.x >= 0)
        {
            rb.MovePosition(rb.position + new Vector2(cornerRayPosX - cornerRayOffsetX, 0));
        }
    }

    bool isWallJumping = false;
    private void WallJump()
    {
        if ((isTouchingWallRight || isTouchingWallLeft) && jumpBufferCounter > 0 && !IsGrounded)
        {
            int wallJumpDir;
            if (isTouchingWallRight)
                wallJumpDir = -1;
            else
                wallJumpDir = 1;

            // isWallJumping = true;
            walljumpTimerCounter = walljumpTime;
            isWallJumping = true;
            isWallGrabbing = false;
            wallDetachCounter = 0f;
            IsJumping = true;
            rb.linearVelocity = new Vector2(wallJumpXSpeed * wallJumpDir, wallJumpYSpeed);
            jumpBufferCounter = 0;
            // Debug.Log("Wall Jump");
        }
    }


    // ???
    private void Move()
    {
        if (!canMove) return;
        float accel = speedAcceleration;
        float decel = SpeedDeceleration;
        float turnAccel = TurningSpeedAcceleration;
        if (walljumpTimerCounter > 0)
        {
            accel *= wallJumpAccelMulti;
            decel *= wallJumpDecelMulti;
            turnAccel *= wallJumpAccelMulti;
        }
        else if (!IsGrounded) // ??????? ??? ????
        {
            accel *= airAccelMulti;
            decel *= airDecelMulti;
            turnAccel *= airAccelMulti;
        }
        // 바라보는 방향 ?��?��?��?�� �? ?��?��?��?��?�� ?��?��
        if (moveInput.x > 0)
        {
            facingDirection = 1;
            transform.localScale = originalScale; // ?��른쪽
        }
        else if (moveInput.x < 0)
        {
            facingDirection = -1;
            Vector3 flippedScale = originalScale;
            flippedScale.x = -originalScale.x;
            transform.localScale = flippedScale; // ?���? (X�? 반전)
        }

        // if (isWallJumping)
        // {
        //     rb.linearVelocityX -= decel * Mathf.Sign(rb.linearVelocity.x) * Time.fixedDeltaTime;
        // // }
        // else
        // {
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
        // }


    }

    // // ??? ???? (BoxCast)
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
        Vector2 leftOrigin = new Vector2(bounds.min.x, bounds.min.y);
        Vector2 rightOrigin = new Vector2(bounds.max.x, bounds.min.y);

        // 아래로 레이캐스트
        RaycastHit2D leftHit = Physics2D.Raycast(leftOrigin, Vector2.down, extraHeight, wallLayer);
        RaycastHit2D rightHit = Physics2D.Raycast(rightOrigin, Vector2.down, extraHeight, wallLayer);

        // 디버그용 시각화
        Debug.DrawRay(leftOrigin, Vector2.down * extraHeight, Color.green);
        Debug.DrawRay(rightOrigin, Vector2.down * extraHeight, Color.green);

        bool grounded = (leftHit.collider != null || rightHit.collider != null);

        // 벽 슬라이드 상태일 땐 false 처리
        // bool isWallSliding = (isTouchingWallRight || isTouchingWallLeft) && rb.linearVelocity.y < 0f;
        // if (grounded && isWallSliding)
        //     IsGrounded = false;
        // else
        IsGrounded = grounded;
        // Debug.Log("IsGrounded: " + IsGrounded);

        // 점프 중 상태 해제
        if (IsJumping && rb.linearVelocity.y <= 0)
        {
            IsJumping = false;
            currentGravity = jumpDcceleration;
        }
        if (isWallJumping && rb.linearVelocity.y <= 0)
        {
            isWallJumping = false;
            currentGravity = jumpDcceleration;
        }
    }

    // ???
    private void ApplyGravity()
    {
        float newY;
        if (IsJumping)
        {
            // ???? ?? ???(??? ??)
            newY = rb.linearVelocity.y - jumpDcceleration * Time.fixedDeltaTime;
        }
        else if (isWallJumping)
        {
            newY = rb.linearVelocity.y - jumpDcceleration * Time.fixedDeltaTime;
        }
        else
        {
            // ???? ?? ???(?????? ??)
            // ???? ?? ???(????)???? ???? ?? ???(????)???? ?????????? ????
            if (currentGravity < maxGravity)
                currentGravity += gravityAcceleration * Time.fixedDeltaTime;
            else
                currentGravity = maxGravity;

            newY = rb.linearVelocity.y - currentGravity * Time.fixedDeltaTime;
        }

        // ????? ?????? ??? ????
        if (isTouchingWallRight || isTouchingWallLeft)
            if (newY < -wallSlideMaxSpeed)
                newY = -wallSlideMaxSpeed;

        // y?? ??? ???
        newY = Mathf.Clamp(newY, -maxDownSpeed, maxJumpSpeed);

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);
    }

    private void Jump()
    {
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            // +y?? linearVelocity ????
            IsJumping = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxJumpSpeed);
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
            // Debug.Log("Jumped");
            if (isFastFalling)
                IsJumping = false;
        }
    }

    // ???? ? ???? isJumping = false -> ??? ?????? -> ???? ??????
    private void FastFall()
    {
        if (IsJumping)
        {
            IsJumping = false;
        }
        if (jumpBufferCounter > 0)
            isFastFalling = true;

    }

    // ?? ???? (Raycast)
    private void WallCheck()
    {
        Vector2 origin = transform.position;
        RaycastHit2D hitWallRight = new RaycastHit2D(); // ???????? ????
        RaycastHit2D hitWallLeft = new RaycastHit2D(); // ???????? ????
        hitWallRight = Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, wallLayer);
        Debug.DrawRay(origin, Vector2.right * wallCheckDistance, Color.red);
        hitWallLeft = Physics2D.Raycast(origin, Vector2.left, wallCheckDistance, wallLayer);
        Debug.DrawRay(origin, Vector2.left * wallCheckDistance, Color.red);


        isTouchingWallRight = hitWallRight.collider != null;
        isTouchingWallLeft = hitWallLeft.collider != null;

    }


    // private void WallJump()
    // {
    //     if ((isTouchingWallRight || isTouchingWallLeft) && jumpBufferCounter > 0 && !IsGrounded)
    //     {
    //         int wallJumpDir;
    //         if (isTouchingWallRight)
    //             wallJumpDir = -1;
    //         else
    //             wallJumpDir = 1;

    //         IsJumping = true;
    //         // ✨ 초기값만 저장, 실제 속도는 설정하지 않음
    //         wallJumpInitialVelX = wallJumpXSpeed * wallJumpDir;

    //         // ========== 벽점프 발딛움 시작 ==========
    //         isWallJumping = true;
    //         canMove = false;  // Move() 스킵
    //         wallJumpElapsedTime = 0f;  // 타이머 리셋
    //                                    // =====================================

    //         Debug.Log("Wall Jump");
    //         jumpBufferCounter = 0;  // 연속 점프 방지
    //     }
    // }

    private void Dash()
    {
        if (dashCount <= 0) return;
        if (dashCooldownCounter > 0) return;
        isDashing = true;
        dashCount -= 1;
        ChangeColor();
        dashTimeCounter = dashTime;
        dashCooldownCounter = dashCooldown;
        playerDataLog.OnPlayerUseAbility();
        rb.excludeLayers = LayerMask.GetMask("Breakable");
        // ?��?�� 바라보는 방향?���? ????��
        if (moveInput == Vector2.zero)
            rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0);
        else
            rb.linearVelocity = moveInput.normalized * dashSpeed;
    }

    // ??? ?? ????, ??? ?????? ????? ????
    private void dampAfterDash()
    {
        float dampedSpeedX = rb.linearVelocity.x;
        float dampedSpeedY = rb.linearVelocity.y;
        rb.excludeLayers = 0;
        dampedSpeedX = Mathf.Clamp(dampedSpeedX, -maxSpeedAfterDashX, maxSpeedAfterDashX);
        dampedSpeedY = Mathf.Min(dampedSpeedY, maxSpeedAfterDashUp);
        rb.linearVelocity = new Vector2(dampedSpeedX, dampedSpeedY);
    }

    public void OnEnableSetVelocity(float newVelX, float newVelY, int currentDashCount)
    {
        col = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        currentGravity = jumpDcceleration;
        wallLayer = LayerMask.GetMask("Ground");
        dashCount = currentDashCount;
        ChangeColor();
        // Rigidbody ????
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 0f; // ????? ???? ???

        // 이거필요없잖아 네모로 변신이 될때 적용되는건데 이거 왜만듦
        if (isDashing)
            rb.linearVelocity = Vector2.zero;
        else
            rb.linearVelocity = new Vector2(newVelX, newVelY);
    }


    private void ChangeColor()
    {
        if (spriteRenderer != null)
        {
            if (dashCount <= 0)
            {
                spriteRenderer.color = noDashColor;
            }
            else
            {
                spriteRenderer.color = defaultColor;
            }
        }
    }


    #region 잔상 효과
    private void CreateAfterImage()
    {
        GameObject afterImage = new GameObject("AfterImage");
        afterImage.transform.position = transform.position;
        afterImage.transform.rotation = transform.rotation;
        afterImage.transform.localScale = transform.localScale;

        SpriteRenderer afterImageSR = afterImage.AddComponent<SpriteRenderer>();
        afterImageSR.sprite = spriteRenderer.sprite;
        afterImageSR.color = new Color(1f, 1f, 1f, 0.5f); // 반투명
        afterImageSR.sortingLayerName = spriteRenderer.sortingLayerName;
        afterImageSR.sortingOrder = spriteRenderer.sortingOrder - 1;

        // 안전장치: afterImageLifetime * 2 시간 후 강제 삭제
        Destroy(afterImage, afterImageLifetime * 2f);

        // 잔상 페이드아웃 코루틴 시작
        StartCoroutine(FadeOutAfterImage(afterImageSR, afterImage));
    }

    private System.Collections.IEnumerator FadeOutAfterImage(SpriteRenderer sr, GameObject obj)
    {
        if (sr == null || obj == null) yield break; // null 체크

        float elapsed = 0f;
        Color originalColor = sr.color;

        while (elapsed < afterImageLifetime && sr != null && obj != null)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(originalColor.a, 0f, elapsed / afterImageLifetime);
            sr.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        // 오브젝트가 여전히 존재한다면 삭제
        if (obj != null)
        {
            Destroy(obj);
        }
    }
    #endregion
}
