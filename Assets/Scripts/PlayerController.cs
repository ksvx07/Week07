using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour, IPlayerController
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
    private bool isDashing;
    private int dashCount;
    private bool isFastFalling;
    private int facingDirection = 1; // 1: ?��른쪽, -1: ?���?
    private Vector3 originalScale; // ?���? ?���? ????��

    private void Awake()
    {
        inputActions = new PlayerInput();
        col = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentGravity = jumpDcceleration;
        wallLayer = LayerMask.GetMask("Ground");
        dashCount = maxDashCount;

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
        IsGrounded = false;
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
                dashCount = maxDashCount;
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
    }

    private void FixedUpdate()
    {
        WallCheck();
        DetectGround();
        if (!isDashing)
        {
            Jump();
            CornerCorrection();
            WallJump();
            ApplyGravity();
            Move();
        }


        //Debug.Log($"x: {rb.linearVelocity.x:F2}, y: {rb.linearVelocity.y:F2}");
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


    // ???
    private void Move()
    {
        float accel = speedAcceleration;
        float decel = SpeedDeceleration;
        if (!IsGrounded) // ??????? ??? ????
        {
            accel *= airAccelMulti;
            decel *= airDecelMulti;
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

        float targetX = moveInput.x * maxSpeed;
        float lerpAmount = (moveInput.x != 0 ? accel : decel) * Time.fixedDeltaTime;
        // ?��?�� 방향?�� ?���? ?��로운 X�? ?��?�� 계산
        float newX = Mathf.Lerp(rb.linearVelocity.x, targetX, lerpAmount);
        rb.linearVelocityX = newX;
    }

    // ??? ???? (BoxCast)
    private void DetectGround()
    {
        Bounds bounds = col.bounds;
        float extraHeight = 0.05f;

        RaycastHit2D hit = Physics2D.BoxCast(bounds.center, bounds.size, 0f, Vector2.down,
            extraHeight, wallLayer);

        IsGrounded = hit.collider != null;


        if (IsJumping && rb.linearVelocity.y <= 0)
        {
            IsJumping = false;
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

    // ????
    private void Jump()
    {
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            // +y?? linearVelocity ????
            Debug.Log("Jump!");
            IsJumping = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxJumpSpeed);
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
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

    // ?????? ? ??? ??? ???? linearVelocity ????
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
            Debug.Log("Wall Jump");
        }
    }

    // ???, ??? ?? ??? ???? ??????? linearVelocity?? ?????? moveInput.normalized * dashSpeed??.
    private void Dash()
    {
        if (dashCount <= 0) return;
        if (dashCooldownCounter > 0) return;
        isDashing = true;
        dashCount -= 1;
        dashTimeCounter = dashTime;
        dashCooldownCounter = dashCooldown;

        // ?��?�� 바라보는 방향?���? ????��
        rb.linearVelocity = new Vector2(facingDirection * dashSpeed, 0);
    }

    // ??? ?? ????, ??? ?????? ????? ????
    private void dampAfterDash()
    {
        float dampedSpeedX = rb.linearVelocity.x;
        float dampedSpeedY = rb.linearVelocity.y;
        dampedSpeedX = Mathf.Clamp(dampedSpeedX, -maxSpeedAfterDashX, maxSpeedAfterDashX);
        dampedSpeedY = Mathf.Min(dampedSpeedY, maxSpeedAfterDashUp);
        rb.linearVelocity = new Vector2(dampedSpeedX, dampedSpeedY);
    }

    public void OnEnableSetVelocity(float newVelX, float newVelY)
    {
        col = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        currentGravity = jumpDcceleration;
        wallLayer = LayerMask.GetMask("Ground");
        dashCount = maxDashCount;

        // Rigidbody ????
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 0f; // ????? ???? ???

        rb.linearVelocity = new Vector2(newVelX, newVelY);
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
