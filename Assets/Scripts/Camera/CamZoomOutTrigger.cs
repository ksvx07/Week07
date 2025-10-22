using Unity.VisualScripting;
using UnityEngine;

public class CamZoomOutTrigger : MonoBehaviour
{
    private float _zoomOutSize = 15f;
    private float _zoomInSize = 3f;
    private void OnTriggerStay2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            CameraController.Instance.TriggerZoomOut(_zoomOutSize);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            CameraController.Instance.TriggerZoomIn(_zoomInSize);
        }
    }
}
