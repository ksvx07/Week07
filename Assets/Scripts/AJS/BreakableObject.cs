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
            KirbyController turboMode = collision.GetComponent<KirbyController>();
            if (turboMode != null)
            {
                if (turboMode.TurboMode)
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
