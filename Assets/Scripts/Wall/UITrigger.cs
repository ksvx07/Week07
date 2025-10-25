using System.Collections;
using UnityEngine;

public class UITrigger : MonoBehaviour
{
    [SerializeField] private GameObject targetObject;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Vector3 targetScale;
    private Coroutine scaleCoroutine;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (scaleCoroutine != null)
            {
                StopCoroutine(scaleCoroutine);
            }
            scaleCoroutine = StartCoroutine(ScaleOverTime(targetObject.transform, targetScale, duration));
        }
    }

    private IEnumerator ScaleOverTime(Transform target, Vector3 toScale, float duration)
    {
        Vector3 fromScale = target.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            target.localScale = Vector3.Lerp(fromScale, toScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        target.localScale = toScale;
    }
}
