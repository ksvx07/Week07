using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MissileBodyController : MonoBehaviour
{
    [SerializeField] private GameObject missile;
    [SerializeField] private Transform missileBody;
    [SerializeField] private Transform missileParent;
    [SerializeField] private float shootingCooldown;

    private Quaternion offsetRotation = Quaternion.Euler(0, 0, 90);
    private Vector3 offsetPosition = new Vector3(-0.5f, 0f, 0f);

    private Transform _player => PlayerManager.Instance._currentPlayerPrefab.transform;
    private float _shootingCurrentTime;
    private int _maxEnemy = 5;
    public List<GameObject> _activeMissiles = new();
    public Queue<GameObject> _missilePool = new();

    void Start()
    {
        _shootingCurrentTime = Time.time;
        _activeMissiles.Add(missile);
    }

    void Update()
    {
        if (_activeMissiles.Count > _maxEnemy) return;
        if(_player == null) return;

        var distance = Vector3.Distance(transform.position, _player.position);

        if (distance < 5f)
        {
            Vector2 dir = (_player.position - transform.position);
            float anleZ = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 180f;

            transform.rotation = Quaternion.Euler(0f, 0f, anleZ);

            if (Time.time - _shootingCurrentTime > shootingCooldown)
            {
                ShootingBullet();
            }
        }
    }

    private void ShootingBullet()
    {
        GameObject bullet;

        if (_missilePool.Count > 0)
        {
            bullet = _missilePool.Dequeue();

            // bullet.transform.SetParent(missileParent);

            // bullet.transform.localPosition = new Vector3(-0.83f, 0f, 0f);   // ��ġ �ʱ�ȭ
            // bullet.transform.localRotation = Quaternion.Euler(0, 0, 90);    // ȸ�� �ʱ�ȭ
            bullet.transform.rotation = missileBody.rotation * offsetRotation;
            bullet.transform.position = missileBody.position + missileBody.rotation * offsetPosition;

        }
        else
        {
            bullet = Instantiate(missile, missileBody.position + missileBody.rotation * offsetPosition, missileBody.rotation * offsetRotation, missileParent);
        }

        bullet.GetComponent<FollowMissile>().Init(this);
        // bullet.transform.SetParent(null);

        _shootingCurrentTime = Time.time;
        bullet.SetActive(true);
        _activeMissiles.Add(bullet);
    }


}
