using UnityEngine;

public class CircleSpining : MonoBehaviour
{
    [SerializeField] Rigidbody2D rb;
    [SerializeField] KirbyController kirbyController;

    [Range(0.1f, 10.0f)]
    [Tooltip("한바퀴 도는데 걸리는 최저 시간")] [SerializeField] float rotationTimeInMinSpeed = 1.0f;
    [Range(0.1f, 10.0f)]
    [Tooltip("MaxSpeed에서 한바퀴 도는데 걸리는 시간")] [SerializeField] float rotationTimeInMaxSpeed = 1.0f;
    [Range(0.1f, 10.0f)]
    [Tooltip("TurboSpeed에서 한바퀴 도는데 걸리는 시간")] [SerializeField] float rotationTimeInTurboSpeed = 1.0f;

    void Update()
    {
        // 속도가 0에 근접하면 정지
        if (rb.linearVelocity.magnitude < 0.2f)
        {
            return;
        }

        transform.Rotate(0, 0, -GetCurrentRotationSpeed() * Time.deltaTime);
    }

    private float GetCurrentRotationSpeed()
    {
        if (kirbyController.TurboMode)
        {
            // 터보모드일 때는 터보전용 회전으로
            return 360 / rotationTimeInTurboSpeed;
        }
        float speedRatio = Mathf.Clamp01(Mathf.Abs(rb.linearVelocityX) / kirbyController.MaxSpeed);
        return Mathf.Lerp(360f/rotationTimeInMinSpeed, 360f/ rotationTimeInMaxSpeed, speedRatio);
    }

}
