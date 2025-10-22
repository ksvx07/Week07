using Unity.VisualScripting;
using UnityEngine;

public class FollowMissile : MonoBehaviour
{
    [SerializeField] private float _shootingSpeed = 8f;
    [SerializeField] private float _rotateSpeed = 150f;
    [SerializeField] private GameObject particleEffect;

    private MissileBodyController _missileController;
    private Transform _player => PlayerManager.Instance._currentPlayerPrefab.transform;
    private Rigidbody2D _rb;


    private void Start()
    {
        RespawnManager.Instance.OnPlayerSpawned += PlayerSpawned;
    }

    public void Init(MissileBodyController missileController)
    {
        _missileController = missileController;
        _rb = GetComponent<Rigidbody2D>();

        // 속도, 각속도 초기화
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;

        if (_player != null)
        {
            Vector2 toTarget = (Vector2)_player.position - (Vector2)transform.position;
            float startAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;
            _rb.SetRotation(startAngle);
        }
        else
        {
            _rb.SetRotation(transform.eulerAngles.z);
        }
    }

    void FixedUpdate()
    {
        if (_player == null) return;

        Vector2 toTarget = (Vector2)_player.position - _rb.position;
        if (toTarget.sqrMagnitude < 0.0001f) return; // 너무 가까우면 무시

        float currentAngle = _rb.rotation;
        float targetAngle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f;

        float delta = Mathf.DeltaAngle(currentAngle, targetAngle);
        float maxStep = _rotateSpeed * Time.fixedDeltaTime;
        float step = Mathf.Clamp(delta, -maxStep, maxStep);

        _rb.MoveRotation(currentAngle + step);

        Vector2 forward = _rb.GetRelativeVector(Vector2.up);
        _rb.linearVelocity = forward * _shootingSpeed;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // Triangle 내려찍기 중이면 미사일만 사라지게
            var triangle = collision.GetComponent<TrianglePlayerController>();
            if (triangle != null && triangle.IsDownDash)
            {
                gameObject.SetActive(false);
                Instantiate(particleEffect, transform.position, Quaternion.identity);
                if (_missileController != null)
                {
                    _missileController._missilePool.Enqueue(gameObject);
                    _missileController._activeMissiles.Remove(gameObject);
                }
                return;
            }

            // 그 외에는 플레이어 리스폰 처리
            GameManager.Instance.RespawnPlayer();
        }
    }

    private void PlayerSpawned(Vector3 _noNeed)
    {
        gameObject.SetActive(false);
        if (_missileController != null)
        {
            _missileController._missilePool.Enqueue(gameObject);
            _missileController._activeMissiles.Remove(gameObject);
        }
        return;
    }


}
