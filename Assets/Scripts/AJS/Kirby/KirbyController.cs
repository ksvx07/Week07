using System.Collections;
using UnityEngine;

public class KirbyController : MonoBehaviour, IPlayerController
{
    #region References
    Rigidbody2D _rb;
    KirbyGroundCheck _groundCheck;
    #endregion

    [Header("Movement Stats")]
    [SerializeField, Range(0f, 20f)][Tooltip("�ְ��ӵ�")] private float maxSpeed = 10f;
    [SerializeField, Range(0f, 100f)][Tooltip("�󸶳� ���� �ְ��ӵ��� ����")] private float maxAcceleration = 52f;
    [SerializeField, Range(0f, 100f)][Tooltip("�Է°� ������, �󸶳� ���� ����")] private float maxDecceleration = 52f;
    [SerializeField, Range(1f, 100f)][Tooltip("���� ��ȯ��, �󸶳� ���� ����")] private float maxTurnSpeed = 80f;
    [SerializeField, Range(0f, 100f)][Tooltip("���߿���, �󸶳� ���� �ְ��ӵ��� ����")] private float maxAirAcceleration;
    [SerializeField, Range(0f, 100f)][Tooltip("���߿���, �Է°� ������, �󸶳� ���� ����")] private float maxAirDeceleration; // �ٿ��� AirBreak
    [SerializeField, Range(0f, 100f)][Tooltip("���߿���, ���� ��ȯ��, �󸶳� ���� ����")] private float maxAirTurnSpeed = 80f;// �ٿ��� AirControl

    [Header("Turbo Stats")]
    [SerializeField, Range(0f, 20f)][Tooltip("�ͺ��ӵ�")] private float turboSpeed = 20f;
    [SerializeField, Range(0f, 100f)][Tooltip("���� ��ȯ��, �󸶳� ���� ����")] private float turboDecceleration = 52f;

    [Header("Bounce Settings")]
    [Tooltip("X������ ƨ�� ������ ��")]
    [SerializeField] private float bounceStrength = 5f;
    [Tooltip("Y������ ƨ�� ������ ��")]
    [SerializeField] private float bounceHeight = 10f;
    [Tooltip("ƨ�� ������ ȿ���� ���ӵǴ� �ּҽð�")]
    [SerializeField] private float bounceDuration = 0.3f;
    private bool isBouncing = false; // ���絵 ƨ�� �������� 
    private bool isFixedBouncing = false; // ƨ�� �������� �ּ� ���� �ð�

    [Header("Current State")]
    [SerializeField]
    private LayerMask groundLayer;
    public bool onGround;
    public bool pressingKey; // �̵�Ű�� ������ �ִ��� ����
    private bool turboMode;

    #region Private - Speed Caculation Variables
    private Vector2 desiredVelocity; // �̵��ϰ� �;� �ϴ� Velocity��
    private Vector2 moveVelocity; // ���� �̵��� Velocity ��
    private float directionX; // ������ �ִ� ���� ����: -1, ������ +1
    private float turboDirectionX; // ������ �ִ� ���� ����: -1, ������ +1
    private float maxSpeedChangeAmount; // ���� ���� �� �� �ִ� �ִ� �ӵ� ���淮
    private float acceleration; // ���� ����Ǵ� ���ӵ�
    private float deceleration; // ���� ����Ǵ� ���ӵ�
    private float turnSpeed; // ������ȯ �ӵ�
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
        _groundCheck = GetComponent<KirbyGroundCheck>(); // �� ��Ҵ��� �˱����� ��ũ��Ʈ
    }

    private void OnDisable()
    {
        InitializedCircle();
    }
    private void Update()
    {
        // �Է�Ű�� ������ ���� �ּ� �����ؾ� �ϴ� boucning�ð�
        if (isFixedBouncing)
        {
            return;
        }

        // �Է°��� ������, ���� �������� ������ȯ
        if (directionX != 0)
        {
            transform.localScale = new Vector3(directionX > 0 ? 1 : -1, 1, 1);
            pressingKey = true;
        }
        else
        {
            pressingKey = false;
        }

        // �ٿ ���¿��� �Է��� ������, ��� �ٿ vector ����
        if (isBouncing)
        {
            if (pressingKey) { isBouncing = false; }
            else return;
        }


        if (turboMode)
        {
            // �ͺ� speed�� �̵�
            desiredVelocity = new Vector2(transform.localScale.x, 0f) * turboSpeed;
            turboDirectionX = desiredVelocity.x; // ���� ������ ����
        }
        else
        {
            // ���� ������ �ִ� ���⿡, maxSpeed�� ����, desiredVelocity�� ���ϱ� (�ٷ� maxSpeed�� �������� �ʰ�, ���ӵ� ���η� ���ϱ�)
            desiredVelocity = new Vector2(directionX, 0f) * Mathf.Max(maxSpeed, 0f);
        }
    }

    private void FixedUpdate()
    {
        if (isFixedBouncing) return; // �ּ� �ٿ ���� �ð����� return

        onGround = _groundCheck.GetOnGround();

        // �ٿ ���¿����� ���� ������, �ٿ ���� ����
        if (isBouncing)
        {
            if (onGround) { isBouncing = false; }
            return;
        }
        //���� velocity ���� ��������
        moveVelocity = _rb.linearVelocity;

        if (turboMode)
        {
            // �ͺ� ��忡���� �����̳�, ���� ����
            runWithoutAcceleration();
        }
        else
        {
            runWithAcceleration();
        }
    }

    // Hack: �ӽ÷� Wall tag ����
    private void OnCollisionStay2D(Collision2D collision)
    {
        // �ٿ ���°ų� �ͺ���尡 �ƴϸ� ������ �ʿ� ����
        if (isBouncing || !turboMode) return;
        if ((groundLayer) != 0)
        {
            // ��� �浹 ������ ��ȸ�ϸ� ���� ������ Ȯ���մϴ�.
            foreach (ContactPoint2D contact in collision.contacts)
            {
                Vector2 normal = contact.normal;

                // ���� ������ y ������ 0�� ������� Ȯ���մϴ�.
                if (Mathf.Abs(normal.y) < 0.01f)
                {
                    turboMode = false;
                    // �浹 �� �ٿ �ڷ�ƾ ����
                    StartCoroutine(Bounce(collision));
                    return;
                }
            }
        }
    }


    private void InitializedCircle()
    {
        turboMode = false;
        isFixedBouncing = false;
        isBouncing = false;
    }

    // �ְ��ӵ� ������ ���� ���ӵ� �����
    private void runWithAcceleration()
    {
        // ���߿� �ִ����� ����, ������ ���ӵ�, ���ӵ�, ������ȯ�ӵ� �� ����
        acceleration = onGround ? maxAcceleration : maxAirAcceleration;
        deceleration = onGround ? maxDecceleration : maxAirDeceleration;
        turnSpeed = onGround ? maxTurnSpeed : maxAirTurnSpeed;

        // �̵�Ű�� ��������
        if (pressingKey)
        {
            //���� �̵� x �����, �������� �ϴ� ���� x���� ��ȣ�� �ٸ��ٴ� ����. ����Ű�� �ٲ�ٴ� ������, turnSpeed�� �����Ѵ�
            if (Mathf.Sign(directionX) != Mathf.Sign(moveVelocity.x))
            {
                maxSpeedChangeAmount = turnSpeed * Time.deltaTime;
            }
            else
            {
                //���ٸ�, ������ ���� �������� ���� �ִٴ� ������, acceleration�� �����Ѵ�
                maxSpeedChangeAmount = acceleration * Time.deltaTime;
            }
        }
        else
        {
            //����Ű�� ������ �ִ� ���°� �ƴϸ�, �����ؾ� �ϹǷ�, deceleration�� �����Ѵ�
            maxSpeedChangeAmount = deceleration * Time.deltaTime;
        }

        //���� velocity ����, ���� �Ǵ� velocity ���� ���̸� ���ϵ�, ���� �ִ� �ӵ����淮�� ���� ���� ���� ��ȯ
        moveVelocity.x = Mathf.MoveTowards(moveVelocity.x, desiredVelocity.x, maxSpeedChangeAmount);

        //���� ����� moveVelocity ���� Update�� �����Ѵ�
        _rb.linearVelocity = moveVelocity;
    }

    // ���ӵ� ���� ���� �ٷ� �ְ� �ӵ��� �̵�
    private void runWithoutAcceleration()
    {

        //���� �̵� x �����, �������� �ϴ� ���� x���� ��ȣ�� �ٸ��ٴ� ����. ����Ű�� �ٲ�ٴ� ������, turnSpeed�� �����Ѵ�
        if (Mathf.Sign(turboDirectionX) != Mathf.Sign(moveVelocity.x))
        {
            maxSpeedChangeAmount = turboDecceleration * Time.deltaTime;

            //���� velocity ����, ���� �Ǵ� velocity ���� ���̸� ���ϵ�, ���� �ִ� �ӵ����淮�� ���� ���� ���� ��ȯ
            moveVelocity.x = Mathf.MoveTowards(moveVelocity.x, desiredVelocity.x, maxSpeedChangeAmount);
        }
        else
        {
            // ������ ������ �ܼ��ϰ�, ���� ���� * �ִ�ӵ� linearVelocity ���� Rigidbody ����
            moveVelocity.x = desiredVelocity.x;
        }
        _rb.linearVelocity = moveVelocity;
    }

    private void TurboWithAcceleration()
    {
        // ���ӵ��� ������ ������
        //�ܼ��ϰ�, ���� ���� * �ִ�ӵ� linearVelocity ���� Rigidbody ����
        moveVelocity.x = desiredVelocity.x;
        _rb.linearVelocity = moveVelocity;
    }

    private IEnumerator Bounce(Collision2D collision)
    {
        isFixedBouncing = true;
        isBouncing = true;

        Vector2 normal = Vector2.zero;

        // collision.contacts 배열을 순회하며 조건을 만족하는 normal을 찾습니다.
        foreach (ContactPoint2D contact in collision.contacts)
        {
            // y 성분의 절대값이 0.01f보다 작은 normal을 찾습니다.
            if (Mathf.Abs(contact.normal.y) < 0.01f)
            {
                normal = contact.normal;
                break; // 조건을 만족하는 normal을 찾았으므로 루프를 종료합니다.
            }
        }

        // 조건을 만족하는 normal이 있을 경우에만 나머지 로직을 실행합니다.
        if (normal != Vector2.zero)
        {
            isFixedBouncing = true;
            isBouncing = true;
            Vector2 fixedBounceVelocity = new Vector2(
                normal.x * bounceStrength,
                bounceHeight
            );

            _rb.linearVelocity = fixedBounceVelocity;

            yield return new WaitForSeconds(bounceDuration);
            isFixedBouncing = false;
        }
    }

    #region Public - PlayerInput
    public void OnMoveInput(Vector2 movementInput)
    {
        directionX = movementInput.x;
    }

    public void OnTurboModePressed()
    {
        // �ٿ ���¿����� TurboMode �Ұ���
        if (isBouncing) return;

        turboMode = !turboMode;
    }

    public void OnEnableSetVelocity(float newVelX, float newVelY)
    {
        _rb = GetComponent<Rigidbody2D>();
        _groundCheck = GetComponent<KirbyGroundCheck>(); // �� ��Ҵ��� �˱����� ��ũ��Ʈ
        _rb.linearVelocity = new Vector2(newVelX, newVelY);
    }
    #endregion
}
