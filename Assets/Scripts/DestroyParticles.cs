using UnityEngine;
using System.Collections;

public class DestroyParticles : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(DestroyAfterDelay());
    }

    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(1.1f);
        Destroy(gameObject);
    }
}
