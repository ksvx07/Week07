// BrokenTileFadeOut.cs
using UnityEngine;

public class BrokenTileFadeOut : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private float fadeTime = 1f;
    private float startTime;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        startTime = Time.time;
    }

    private void Update()
    {
        float elapsed = Time.time - startTime;
        if (elapsed > fadeTime)
        {
            Destroy(gameObject);
            return;
        }

        // ✅ 알파 페이드 아웃
        Color color = spriteRenderer.color;
        color.a = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
        spriteRenderer.color = color;
    }
}