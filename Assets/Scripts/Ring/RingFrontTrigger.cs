using UnityEngine;

public class RingFrontTrigger : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            RingTrigger ringTrigger = GetComponentInParent<RingTrigger>();
            if (ringTrigger != null)
            {
                ringTrigger.OnRingFrontEnter();
            }
        }
    }
}
