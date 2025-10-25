using UnityEngine;

public class BreakableObject : MonoBehaviour
{
    [SerializeField] private GameObject particleEffect;

    private void Start()
    {
        RespawnManager.Instance.OnPlayerSpawned += PlayerSpawned;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (collision.TryGetComponent<KirbyController>(out KirbyController turboMode))
            {
                if (turboMode.TurboMode)
                {
                    TurbomodeDestoy();
                }
            }
            else if (collision.TryGetComponent<SquareController>(out SquareController dashMode))
            {
                if (dashMode.isDashing)
                {
                    TurbomodeDestoy();
                }
            }
        }
    }

    private void PlayerSpawned(Vector3 _noNeed)
    {
        gameObject.SetActive(true);
    }

    public void TurbomodeDestoy()
    {
        if (particleEffect != null)
        {
            Instantiate(particleEffect, transform.position, Quaternion.identity, null);
        }
        gameObject.SetActive(false);
    }
}
