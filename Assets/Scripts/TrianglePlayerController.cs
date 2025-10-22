using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class TrianglePlayerController : MonoBehaviour, IPlayerController
{


    #region 컴포넌트
    private PlayerInput inputActions;
    private Vector2 moveInput;
    private Rigidbody2D rb;
    private PolygonCollider2D col;
    private SpriteRenderer spriteRenderer; // 추가
    #endregion

    #region 상태 변수
    // Inspector variables for tuning
    [Header("Move")]
    [SerializeField] private float maxSpeed = 5f; //최대 이동 속도
    [SerializeField] private float speedAcceleration = 5f; //이동 가속도
    [SerializeField] private float SpeedDeceleration = 5f; //이동 감속도
    [Header("Triangle Hop")]
    [SerializeField] private float hopHeight = 5f; //깡충깡충 점프 높이
    [SerializeField] private float hopCooldownTime = 0.25f; //

    [Header("Jump / Gravity")]
    [SerializeField] private float maxJumpSpeed = 5f; //최대 점프 속도
    [SerializeField] private float jumpDcceleration = 5f; //점프 감속도
    [SerializeField] private float maxGravity = 5f; //최대 중력
    [SerializeField] private float gravityAcceleration = 5f; //중력 가속도
    [SerializeField] private float maxDownSpeed = 5f; //최대 낙하 속도
    [SerializeField] private float coyoteTime = 0.1f;       //코요테 타이머
    [SerializeField] private float jumpBufferTime = 0.1f;   //점프 버퍼 타이머
    [SerializeField] private float cornerRayPosX = 0.3f;
    [SerializeField] private float cornerRayOffsetX = 0.1f;
    [SerializeField] private float cornerRayLength = 0.1f;

    [Header("Wall Jump")]
    [SerializeField] private float wallCheckDistance = 0.4f; //벽 체크 거리
    [SerializeField] private float wallJumpXSpeed = 5f; //벽 점프 X 속도
    [SerializeField] private float wallJumpYSpeed = 5f; //벽 점프 Y 속도
    [SerializeField] private float wallSlideMaxSpeed = 5f; //벽 미끄럼 최대 속도

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 5f; //대쉬 속도
    [SerializeField] private float dashTime = 0.5f;  //대쉬 시간
    [SerializeField] private float maxSpeedAfterDashX = 5f; //대쉬 후 최대 X 속도
    [SerializeField] private float maxSpeedAfterDashUp = 5f; //대쉬 후 최대 Y 속도
    [SerializeField] private int maxDashCount = 1; //최대 대쉬 횟수

    [SerializeField] private GameObject afterImagePrefab; // 잔상 프리팹
    [SerializeField] private float afterImageLifetime = 0.3f; // 잔상 지속 시간
    [SerializeField] private float afterImageSpawnRate = 0.05f; // 잔상 생성 간격
    private float afterImageTimer; // 잔상 생성 타이머

    [Header("AirTimeMultiplier")]
    [SerializeField] private float airAccelMulti = 0.65f; //공중 가속도 멀티플라이어
    [SerializeField] private float airDecelMulti = 0.65f; //공중 감속도 멀티플라이어

    #endregion

    #region 내부 변수
    private LayerMask wallLayer; //벽 레이어 마스크(Ground)

    private float currentGravity; //현재 중력 값
    private float coyoteTimeCounter; // 코요테 타이머 카운터
    private float jumpBufferCounter; // 점프 버퍼 카운터
    private float dashTimeCounter; //대쉬 타이머 카운터
    private float hopCooldown = 0f; // 이동 뛰기 쿨다운 추가

    private float jumpCooldownTime = 0.1f;
    private float jumpCooldownTimeCounter = 0f;

    #endregion

    #region 상태 플래그
    private bool isGrounded; //땅에 닿았는지 여부
    private bool isJumping; //점프 중인지 여부
    private bool isTouchingWall; //벽에 닿았는지 여부
    private bool isDashing; //대쉬 중인지 여부
    private bool isFastFalling; //빠른 낙하 중인지 여부
    #endregion

    private int dashCount; //남은 대쉬 횟수
    private int facingDirection = 1; // 1: ?��른쪽, -1: ?���?
    private Vector3 originalScale; // ?���? ?���? ????��
    #region 초기화
    private void Awake()
    {
        inputActions = new PlayerInput();
        col = GetComponent<PolygonCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentGravity = jumpDcceleration;
        wallLayer = LayerMask.GetMask("Ground");
        dashCount = maxDashCount;

        // ?���? ?���? ????��
        originalScale = transform.localScale;

        // ?���? ?���? ????��
        originalScale = transform.localScale;

        // Rigidbody 설정
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 0f; // 중력 스케일 초기화
    }
    #endregion

    #region 입력 이벤트 등록/해제
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
        moveInput = Vector2.zero;
        inputActions.Player.Disable();
        isGrounded = false;
    }
    #endregion

    #region 입력 처리
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
        //Dash();
        TriangleSpecialAbility();
    }
    #endregion

    #region Update/FixedUpdate
    private void Update()
    {
        jumpCooldownTimeCounter -= Time.deltaTime;
        jumpBufferCounter -= Time.deltaTime;
        if (isGrounded)
        {
            if (jumpCooldownTimeCounter < 0)
            {
                coyoteTimeCounter = coyoteTime;
                dashCount = maxDashCount;
            }


            // 땅에 닿으면 찍기 종료
            if (isDashing)
            {
                isDashing = false;
                dampAfterDash();
            }

        }
        else if (MathF.Abs(rb.linearVelocity.y) <= 0.05f)
        {
            if (isDashing)
            {
                isDashing = false;
                dampAfterDash();
            }
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }


        if (isDashing)
        {
            dashTimeCounter -= Time.deltaTime;
            // 잔상 효과 생성
            afterImageTimer -= Time.deltaTime;
            if (afterImageTimer <= 0)
            {
                CreateAfterImage();
                afterImageTimer = afterImageSpawnRate;
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
            CornerCorrection();
            WallJump();
            ApplyGravity();
            Move();
        }
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


    #endregion

    #region 바닥감지
    private void DetectGround()
    {
        Bounds bounds = col.bounds;
        float extraHeight = 0.03f;
        float rayOffset = 0.001f; // 레이 간격 조정 (콜라이더 크기에 맞게 조정)

        // 중앙에서 좌우로 약간 떨어진 두 지점
        Vector2 leftPoint = new Vector2(bounds.center.x - rayOffset, bounds.min.y);
        Vector2 rightPoint = new Vector2(bounds.center.x + rayOffset, bounds.min.y);

        RaycastHit2D hitLeft = Physics2D.Raycast(leftPoint, Vector2.down, extraHeight, wallLayer);
        RaycastHit2D hitRight = Physics2D.Raycast(rightPoint, Vector2.down, extraHeight, wallLayer);
        Debug.DrawRay(leftPoint, Vector2.down * extraHeight, Color.blue);
        Debug.DrawRay(rightPoint, Vector2.down * extraHeight, Color.blue);

        // 둘 중 하나라도 바닥에 닿으면 grounded 상태
        isGrounded = (hitLeft.collider != null || hitRight.collider != null) && rb.linearVelocity.y <= 0.01f;
        // if (jumpCooldownTimeCoutner > 0)
        // {
        //     isGrounded = false;
        // }
        if (isJumping && rb.linearVelocity.y <= 0)
        {
            isJumping = false;
            currentGravity = jumpDcceleration;
        }
    }
    #endregion

    #region 이동
    private void Move() //삼각형 이동
    {
        if (hopCooldown > 0)
        {
            hopCooldown -= Time.fixedDeltaTime;
            return; // 쿨다운 중에는 이동하지 않음
        }

        float accel = speedAcceleration;
        float decel = SpeedDeceleration;
        if (!isGrounded) //공중에서 이동 시 가속도, 감속도 감소
        {
            accel *= airAccelMulti;
            decel *= airDecelMulti;
        }
        float targetX = moveInput.x * maxSpeed;
        float lerpAmount = (moveInput.x != 0 ? accel : decel) * Time.fixedDeltaTime;
        // 이동 방향에 따라 속도 보간
        float newX = Mathf.Lerp(rb.linearVelocity.x, targetX, lerpAmount);

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
        if (moveInput.x != 0 && isGrounded && !isJumping)
        {
            rb.linearVelocity = new Vector2(newX, hopHeight);
            hopCooldown = hopCooldownTime; // 쿨다운 시간 설정
        }
        else
        {
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }
    }
    #endregion

    #region 중력적용
    private void ApplyGravity()
    {
        float newY;
        if (isJumping)
        {
            newY = rb.linearVelocity.y - jumpDcceleration * Time.fixedDeltaTime;
        }
        else
        {
            if (currentGravity < maxGravity)
                currentGravity += gravityAcceleration * Time.fixedDeltaTime;
            else
                currentGravity = maxGravity;

            newY = rb.linearVelocity.y - currentGravity * Time.fixedDeltaTime;
        }

        if (isTouchingWall)
            if (newY < -wallSlideMaxSpeed)
                newY = -wallSlideMaxSpeed;

        newY = Mathf.Clamp(newY, -maxDownSpeed, maxJumpSpeed);

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, newY);
    }
    #endregion


    #region 점프
    private void Jump()
    {
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0)
        {
            jumpCooldownTimeCounter = jumpCooldownTime;
            isJumping = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, maxJumpSpeed);
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
            if (isFastFalling)
                isJumping = false;
        }
    }
    #endregion

    #region ?
    private void FastFall()
    {
        if (isJumping)
        {
            isJumping = false;
            //rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
        }
        if (jumpBufferCounter > 0)
            isFastFalling = true;
    }
    #endregion

    #region 벽감지
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
    #endregion

    #region 벽점프
    private void WallJump()
    {
        if (isTouchingWall && jumpBufferCounter > 0 && !isGrounded)
        {
            isJumping = true;
            rb.linearVelocity = new Vector2(wallJumpXSpeed * -Mathf.Sign(moveInput.x), wallJumpYSpeed);
            Debug.Log("Wall Jump");
        }
    }
    #endregion

    #region 대쉬
    private void Dash()
    {
        if (moveInput == Vector2.zero) return;
        if (dashCount <= 0) return;
        isDashing = true;
        dashCount -= 1;
        dashTimeCounter = dashTime;
        rb.linearVelocity = moveInput.normalized * dashSpeed;
    }
    #endregion

    #region 대쉬 후 감속
    private void dampAfterDash()
    {
        float dampedSpeedX = rb.linearVelocity.x;
        float dampedSpeedY = rb.linearVelocity.y;
        dampedSpeedX = Mathf.Clamp(dampedSpeedX, -maxSpeedAfterDashX, maxSpeedAfterDashX);
        dampedSpeedY = Mathf.Min(dampedSpeedY, maxSpeedAfterDashUp);
        rb.linearVelocity = new Vector2(dampedSpeedX, dampedSpeedY);
    }
    #endregion

    #region 세모 특수 능력
    private void TriangleSpecialAbility()
    {
        if (dashCount <= 0) return;
        if (isGrounded) return; // 이미 땅에 있으면 사용 불가

        // 항상 아래 방향으로만 대시
        Vector2 dashDirection = Vector2.down;

        isDashing = true;
        dashCount -= 1;
        rb.linearVelocity = dashDirection * dashSpeed;
    }


    // 대시 중 적과의 충돌 감지 (물리적 충돌)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDashing && other.CompareTag("Enemy"))
        {
            // 적을 파괴
            other.gameObject.SetActive(false);
            Debug.Log("Enemy destroyed by triangle dash!");
        }
    }
    #endregion


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

    #region 변신
    public void OnEnableSetVelocity(float newVelX, float newVelY)
    {
        col = GetComponent<PolygonCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        currentGravity = jumpDcceleration;
        wallLayer = LayerMask.GetMask("Ground");
        dashCount = maxDashCount;

        // Rigidbody 설정
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.gravityScale = 0f; // 중력 스케일 초기화
        if (!isDashing)
            rb.linearVelocity = new Vector2(newVelX, newVelY);
    }
    #endregion

    public bool IsDownDash => isDashing && rb.linearVelocity.y < 0;
}
