using UnityEngine;

public class KirbyTurboMode : MonoBehaviour
{
    #region Reference
    Rigidbody2D rb;
    KirbyController kirbyController;
    [SerializeField] SpriteRenderer spriteRenderer;
    #endregion
    [Header("Trail")]
    [SerializeField] private TrailRenderer trail;
    private Color normalColor;
    private void Awake()
    {
        normalColor = spriteRenderer.color;
    }
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        kirbyController = GetComponent<KirbyController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (kirbyController.TurboMode)
        {
            TurboModeActive();
        }
        else
        {
            TurboModeDeActive();
        }
    }
    private void TurboModeActive()
    {
        trail.emitting = true;
        rb.excludeLayers = LayerMask.GetMask("Breakable");
        spriteRenderer.color = Color.red;
    }

    private void TurboModeDeActive()
    {
        trail.emitting = false;
        rb.excludeLayers = 0;
        spriteRenderer.color = normalColor;
    }
}
