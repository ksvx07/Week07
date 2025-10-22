using UnityEngine;
using UnityEngine.InputSystem;

public class CircleController : MonoBehaviour
{
    private PlayerInput inputActions;
    private Vector2 moveInput;
    private Rigidbody2D rb;
    private CircleCollider2D col;

    // Inspector 조절 가능 변수들
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
    [SerializeField] private float coyoteTime = 0.1f;       // 코요테 타임 길이
    [SerializeField] private float jumpBufferTime = 0.1f;   // 점프 버퍼 길이

    [Header("Wall Jump")]
    [SerializeField] private float wallCheckDistance = 0.4f;
    [SerializeField] private float wallJumpXSpeed = 5f;
    [SerializeField] private float wallJumpYSpeed = 5f;
    [SerializeField] private float wallSlideMaxSpeed = 5f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 5f;
    [SerializeField] private float dashTime = 0.5f;
    [SerializeField] private float maxSpeedAfterDashX = 5f;
    [SerializeField] private float maxSpeedAfterDashUp = 5f;
    [SerializeField] private int maxDashCount = 1;

    [Header("AirTimeMultiplier")]
    [SerializeField] private float airAccelMulti = 0.65f;
    [SerializeField] private float airDecelMulti = 0.65f;

    private LayerMask wallLayer;

    private float currentGravity;
    private float coyoteTimeCounter; // 땅을 떠난 후 남은 점프 가능 시간
    private float jumpBufferCounter; // 점프 입력 유지 시간
    private float dashTimeCounter;
    // 내부 상태
    private bool isGrounded;
    private bool isJumping;
    private bool isTouchingWall;
    private bool isDashing;
    private int dashCount;

    // Hack : CircleSpiing용 변수
    public float MaxSpeed
    {
        get { return maxSpeed; }
    }


    private void Awake()
    {
        inputActions = new PlayerInput();
        col = GetComponent<CircleCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        currentGravity = jumpDcceleration;
        wallLayer = LayerMask.GetMask("Ground");
        dashCount = maxDashCount;

        // Rigidbody 설정
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 0f; // 중력은 직접 처리
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Move.performed += OnMove;
        inputActions.Player.Move.canceled += OnMove;
        inputActions.Player.Jump.started += OnJump;
        inputActions.Player.Jump.canceled += OffJump;
        inputActions.Player.Dash.performed += OnDash;
    }

    private void OnDisable()
    {
        inputActions.Player.Move.performed -= OnMove;
        inputActions.Player.Move.canceled -= OnMove;
        inputActions.Player.Jump.started -= OnJump;
        inputActions.Player.Jump.canceled -= OffJump;
        inputActions.Player.Dash.performed -= OnDash;
        inputActions.Player.Disable();
    }

    private void OnMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        jumpBufferCounter = jumpBufferTime;
    }

    private void OffJump(InputAction.CallbackContext ctx)
    {
        //FastFall();
    }

    private void OnDash(InputAction.CallbackContext ctx)
    {
        Dash();
    }

    private void Update()
    {
        TimeCounters();
    }

    // 시간 카운터들
    private void TimeCounters()
    {
        // 점프 버퍼 (예약) & 코요테 타임
        jumpBufferCounter -= Time.deltaTime;
        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            dashCount = maxDashCount;
        }else  coyoteTimeCounter -= Time.deltaTime;

        // 일정 시간 대시함, 대시 끝나면 Damping 줌
        if (isDashing)
        {
            dashTimeCounter -= Time.deltaTime;
            if (dashTimeCounter < 0)
            {
                isDashing = false;
                dampAfterDash();
            }
        }
    }

    private void FixedUpdate()
    {
        WallCheck();
        DetectGround();
        if (!isDashing)
        {
            Jump();
            WallJump();
            ApplyGravity();
            Move();
        }
    }

    // 이동
    private void Move()
    {
        float accel = speedAcceleration;
        float decel = SpeedDeceleration;
        if (!isGrounded) // 공중이면 배수 적용
        {
            accel *= airAccelMulti;
            decel *= airDecelMulti;
        }
        float targetX = moveInput.x * maxSpeed;
        float lerpAmount = (moveInput.x != 0 ? accel : decel) * Time.fixedDeltaTime;
        // 속도가 빠를수록 가속도 감소
        float newX = Mathf.Lerp(rb.linearVelocity.x, targetX, lerpAmount);
        rb.linearVelocityX = newX;       
    }

    // 바닥 감지 (BoxCast)
    private void DetectGround()
    {
        Bounds bounds = col.bounds;
        float extraHeight = 0.05f;

        RaycastHit2D hit = Physics2D.BoxCast(bounds.center, bounds.size, 0f, Vector2.down,
            extraHeight, wallLayer);

        isGrounded = hit.collider != null;


        if (isJumping && rb.linearVelocity.y <= 0)
        {
            isJumping = false;
            currentGravity = jumpDcceleration;
        }
    }

    // 중력
    private void ApplyGravity()
    {
        float newY;
        if (isJumping)
        {
            // 점프 중 중력(올라갈 때)
            newY = rb.linearVelocityY - jumpDcceleration * Time.fixedDeltaTime;
        }
        else
        {
            // 점프 후 중력(떨어질 때)
            // 점프 중 중력(약함)에서 점프 후 중력(강함)으로 연속적으로 증가
            if (currentGravity < maxGravity)
                currentGravity += gravityAcceleration * Time.fixedDeltaTime;
            else
                currentGravity = maxGravity;

            newY = rb.linearVelocityY - currentGravity * Time.fixedDeltaTime;
        }

        // 벽잡고 있으면 중력 낮음
        if (isTouchingWall)
            if (newY < -wallSlideMaxSpeed)
                newY = -wallSlideMaxSpeed;

        // y축 최대 속도
        newY = Mathf.Clamp(newY, -maxDownSpeed, maxJumpSpeed);

        rb.linearVelocityY = newY;
    }

    // 점프
    private void Jump()
    {
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            // +y로 linearVelocity 설정
            Debug.Log("Jump!");
            isJumping = true;
            rb.linearVelocityY = maxJumpSpeed;
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
        }
    }

    // 점프 키 때면 isJumping = false -> 중력 강해짐 -> 빨리 떨어짐
    private void FastFall()
    {
        if (isJumping)
        {
            isJumping = false;
        }
    }

    // 벽 감지 (Raycast)
    private void WallCheck()
    {
        Vector2 origin = transform.position;
        RaycastHit2D hitWall = new RaycastHit2D(); // 기본값으로 초기화

        if (moveInput.x > 0)
        {
            hitWall = Physics2D.Raycast(origin, Vector2.right, wallCheckDistance, wallLayer);
            Debug.DrawRay(origin, Vector2.right * wallCheckDistance, Color.red);
        }
        else if (moveInput.x < 0)
        {
            hitWall = Physics2D.Raycast(origin, Vector2.left, wallCheckDistance, wallLayer);
            Debug.DrawRay(origin, Vector2.left * wallCheckDistance, Color.red);
        }

        isTouchingWall = hitWall.collider != null;
    }

    // 벽점프 키 입력 반대 위로 linearVelocity 설정
    private void WallJump()
    {
        if (isTouchingWall && jumpBufferCounter > 0 && !isGrounded)
        {
            isJumping = true;
            rb.linearVelocity = new Vector2(wallJumpXSpeed * -Mathf.Sign(moveInput.x), wallJumpYSpeed);
            Debug.Log("Wall Jump");
        }
    }

    // 대시, 대시 중 모든 변수 무시하고 linearVelocity는 무조건 moveInput.normalized * dashSpeed됨.
    private void Dash()
    {
        if (moveInput == Vector2.zero) return;
        if (dashCount <= 0) return;
        isDashing = true;
        dashCount -= 1;
        dashTimeCounter = dashTime;
        rb.linearVelocity = moveInput.normalized * dashSpeed;
    }

    // 대시 후 댐핑, 대시 끝나면 최대속도 조절
    private void dampAfterDash()
    {
        float dampedSpeedX = rb.linearVelocityX;
        float dampedSpeedY = rb.linearVelocityY;
        dampedSpeedX = Mathf.Clamp(dampedSpeedX, -maxSpeedAfterDashX, maxSpeedAfterDashX);
        dampedSpeedY = Mathf.Min(dampedSpeedY, maxSpeedAfterDashUp);
        rb.linearVelocity = new Vector2(dampedSpeedX, dampedSpeedY);
    }
}
