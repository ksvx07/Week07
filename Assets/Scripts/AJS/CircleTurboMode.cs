using UnityEngine;
using UnityEngine.InputSystem.XR;

public class CircleTurboMode : MonoBehaviour
{
    #region Reference
    Rigidbody2D rb;
    KirbyController kirbyController;
    #endregion
    [Header("Trail")]
    [SerializeField] private TrailRenderer trail;

    private bool turboMode = false;
    public bool TurboMode
    {
        get { return turboMode; }
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        kirbyController = GetComponent<KirbyController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (kirbyController.MaxSpeed - rb.linearVelocity.magnitude < 0.2f)
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
        turboMode = true;
        rb.excludeLayers = LayerMask.GetMask("Breakable");
    }

    private void TurboModeDeActive()
    {
        trail.emitting = false;
        turboMode = false;
        rb.excludeLayers = 0;
    }
}
