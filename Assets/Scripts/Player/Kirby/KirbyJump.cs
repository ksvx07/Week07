using UnityEngine;

public class KirbyJump : MonoBehaviour
{
    #region References
    Rigidbody2D _rb;
    KirbyGroundCheck _groundCheck;
    #endregion
    [HideInInspector] public Vector2 jumpVelocity; // ���� �����, velocity�� ���� ������, ���� ���� �� velocity���� ����մϴ�

    [Header("Jump Stats")]
    [Tooltip("���� ����")]
    public float jumpHeight = 10f;  // ���ϴ� �ִ� ���� ����
    [Tooltip("�ְ� ���̱��� �ɸ��� �ð�, 2��� �� ���� �ð�")]
    public float timeToJumpApex = 1.2f;  // �����δ� ������ �� �ɸ��� �ð�, (�ϰ��� �߷°� ������ ��� 2��� �� ���� �ð�)

    public float fixedGravity; // �������� ���� ���� �⺻ �߷�

    private bool desiredJump; // ������ư�� ������ true, ���� ������ ����� �Ŀ� false
    private bool isGround; // ������ư�� ������ true, ���� ������ ����� �Ŀ� false
    private float _jumpForce; //  ���� ������ ���� ���̶�, ���� �� �߷°��� �������� ������������ �ʿ��� �Ŀ� ���

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
        // ���� �������� Vector ���� �����ɴϴ�
        jumpVelocity = _rb.linearVelocity;

        //desiredJump�� true �� �濡��, ��� ���� ������ �õ� �մϴ�
        if (desiredJump)
        {
            setJumpGravity();
            // ���� ����� �����ؾ� �� jumpVelocity ���� ��� �� ���� �մϴ�
            PerformJump();

            // ����� jumpVelocity ������ linearVelocity ���� ( ���� ���� �Ϸ�)
            _rb.linearVelocity = jumpVelocity;

            //Skip gravity calculations this frame, so currentlyJumping doesn't turn off
            //This makes sure you can't do the coyote time double jump bug
            return;
        }
    }

    #region Private Methods

    /// <summary>
    /// ������ ���� ���� ���� ���� �ð��� ���� �˸��� �߷°��� ���� �����մϴ�
    /// </summary>
    private void setJumpGravity()
    {
        // ���� ������ ���� ���̶�, ���� �ð��� �������� ������������ �߷��� �缳��
        // �����ϰ� �������� ��ӵ� ����
        Vector2 newGravity = new Vector2(0, (-2 * jumpHeight) / (timeToJumpApex * timeToJumpApex));
        _rb.gravityScale = (newGravity.y / Physics2D.gravity.y);
    }

    private void PerformJump()
    {
        // ���� ���� �Ϸ�
        desiredJump = false;

        // �����ϰ� �������� ���� �߷°��� ��������, ������ ���� ������ �����ϱ� ���� �ʿ��� ������ ��� ����
        _jumpForce = Mathf.Sqrt(-2f * Physics2D.gravity.y * _rb.gravityScale * jumpHeight);

        // Player�� ��, �Ǵ� �Ʒ��� �̵� ���� ��, ������ ���� ���� ���� ����ϱ� ���� �� ���� ( ���� ���� ���� ���)
        // Player�� ���� velocity���� �������, ������ ������ ������ �����ϰ� ���ݴϴ�

        // Player�� ���� �̵���
        if (jumpVelocity.y > 0f)
        {
            // �̵��ϴ� �ӵ��� ������ �����Ŀ����� ������, _jumpForce �� 0 (�߰��� ���� �ö��� ����)
            // �׷��� ������, �̵��ϴ� �ӵ��� _jumpForce�� ���� ���� �ش� ������ _jumpForce �缳��
            _jumpForce = Mathf.Max(_jumpForce - jumpVelocity.y, 0f);
        }
        // Player�� �Ʒ��� �̵���
        else if (jumpVelocity.y < 0f)
        {
            // _jumpForce�� �Ʒ��� �ϰ��ϴ� velcoity ���밪 ��ŭ ���� ����, ������ ���̸�ŭ �����ϰ� �����
            _jumpForce += Mathf.Abs(_rb.linearVelocityY);
        }

        // ���� _jumpForce�� ����
        jumpVelocity.y += _jumpForce;
    }

    #endregion

    #region Public - PlayerInput
    public void OnJumpClicked()
    {
        if (isGround)
        {
            // ����Ű�� �������� Ȯ��
            desiredJump = true;
        }
    }
    #endregion
}
