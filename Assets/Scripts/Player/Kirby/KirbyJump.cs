using UnityEngine;

public class KirbyJump : MonoBehaviour
{
    #region References
    Rigidbody2D _rb;
    KirbyGroundCheck _groundCheck;
    #endregion
    [HideInInspector] public Vector2 jumpVelocity; // 점프 실행시, velocity의 값을 가져와, 새로 적용 할 velocity값을 계산합니다

    [Header("Jump Stats")]
    [Tooltip("점프 높이")]
    public float jumpHeight = 10f;  // 원하는 최대 점프 높이
    [Tooltip("최고 높이까지 걸리는 시간, 2배시 총 점프 시간")]
    public float timeToJumpApex = 1.2f;  // 실제로는 떨어질 때 걸리는 시간, (하강시 중력값 동일할 경우 2배시 총 점프 시간)

    public float fixedGravity; // 점프하지 않을 때의 기본 중력

    private bool desiredJump; // 점프버튼을 누르면 true, 실제 점프가 실행된 후에 false
    private bool isGround; // 점프버튼을 누르면 true, 실제 점프가 실행된 후에 false
    private float _jumpForce; //  내가 설정한 점프 높이랑, 구한 위 중력값을 바탕으로 물리공식으로 필요한 파워 계산

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _groundCheck = GetComponent<KirbyGroundCheck>();
    }
    private void Update()
    {
        if (_groundCheck.GetOnGround())
        {
            isGround = true;
            _rb.gravityScale = fixedGravity;
        }
        else
        {
            isGround = false;
            setJumpGravity();
        }

    }

    private void OnDisable()
    {
        isGround = false;
    }

    private void FixedUpdate()
    {
        // 현재 프레임의 Vector 값을 가져옵니다
        jumpVelocity = _rb.linearVelocity;

        //desiredJump을 true 일 경에는, 계속 점프 실행을 시도 합니다
        if (desiredJump)
        {
            setJumpGravity();
            // 점프 수행시 적용해야 할 jumpVelocity 값을 계산 후 적용 합니다
            PerformJump();

            // 계산한 jumpVelocity 값으로 linearVelocity 변경 ( 점프 실행 완료)
            _rb.linearVelocity = jumpVelocity;

            //Skip gravity calculations this frame, so currentlyJumping doesn't turn off
            //This makes sure you can't do the coyote time double jump bug
            return;
        }
    }

    #region Private Methods

    /// <summary>
    /// 설정한 점프 높이 값과 점프 시간을 토대로 알맞은 중력값을 새로 설정합니다
    /// </summary>
    private void setJumpGravity()
    {
        // 내가 설정한 점프 높이랑, 점프 시간을 바탕으로 물리공식으로 중력을 재설정
        // 지루하고 현학적인 등가속도 공식
        Vector2 newGravity = new Vector2(0, (-2 * jumpHeight) / (timeToJumpApex * timeToJumpApex));
        _rb.gravityScale = (newGravity.y / Physics2D.gravity.y);
    }

    private void PerformJump()
    {
        // 점프 수행 완료
        desiredJump = false;

        // 지루하고 현학적인 현재 중력값을 바탕으로, 설정한 높이 값까지 도달하기 위해 필요한 물리값 계산 공식
        _jumpForce = Mathf.Sqrt(-2f * Physics2D.gravity.y * _rb.gravityScale * jumpHeight);

        // Player가 위, 또는 아래로 이동 중일 때, 점프를 실행 했을 때를 대비하기 위한 값 조정 ( 더블 점프 같은 경우)
        // Player의 현재 velocity값과 상관없이, 정해진 높이의 점프를 실행하게 해줍니다

        // Player가 위로 이동시
        if (jumpVelocity.y > 0f)
        {
            // 이동하는 속도가 적용할 점프파워보다 높으면, _jumpForce 값 0 (추가로 위로 올라가지 않음)
            // 그렇지 않으면, 이동하는 속도에 _jumpForce를 값을 빼고 해당 값으로 _jumpForce 재설정
            _jumpForce = Mathf.Max(_jumpForce - jumpVelocity.y, 0f);
        }
        // Player가 아래로 이동시
        else if (jumpVelocity.y < 0f)
        {
            // _jumpForce에 아래로 하강하는 velcoity 절대값 만큼 값을 더해, 정해진 높이만큼 점프하게 만들기
            _jumpForce += Mathf.Abs(_rb.linearVelocityY);
        }

        // 최종 _jumpForce값 더함
        jumpVelocity.y += _jumpForce;
    }

    #endregion

    #region Public - PlayerInput
    public void OnJumpClicked()
    {
        if (isGround)
        {            
        // 점프키를 눌렀음을 확인
        desiredJump = true;
        }
    }
    #endregion
}
