using UnityEngine;

public enum SwitchDirection
{
    LeftToRight,
    RightToLeft,
    TopToBottom,
    BottomToTop
}

public class StageSwitch : MonoBehaviour
{
    [SerializeField] private int BeforeId;
    [SerializeField] private int AfterId;
    [SerializeField] private SwitchDirection direction;

    private CameraClamp Clamp => CameraController.Instance.Clamp;
    private Transform Player => PlayerManager.Instance._currentPlayerPrefab.transform;

    private float prevPlayerX;
    private float prevPlayerY;
    private bool isPlayerInside;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = true;
            prevPlayerX = Player.position.x;
            prevPlayerY = Player.position.y;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            isPlayerInside = false;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isPlayerInside || Player == null) return;
        if (BeforeId == 0 || AfterId == 0) return;

        switch (direction)
        {
            case SwitchDirection.LeftToRight:
                if (prevPlayerX <= transform.position.x && Player.position.x > transform.position.x)
                    Clamp.SetMapBounds(AfterId);
                else if (prevPlayerX >= transform.position.x && Player.position.x < transform.position.x)
                    Clamp.SetMapBounds(BeforeId);
                break;
            case SwitchDirection.RightToLeft:
                if (prevPlayerX >= transform.position.x && Player.position.x < transform.position.x)
                    Clamp.SetMapBounds(AfterId);
                else if (prevPlayerX <= transform.position.x && Player.position.x > transform.position.x)
                    Clamp.SetMapBounds(BeforeId);
                break;
            case SwitchDirection.TopToBottom:
                if (prevPlayerY >= transform.position.y && Player.position.y < transform.position.y)
                    Clamp.SetMapBounds(AfterId);
                else if (prevPlayerY <= transform.position.y && Player.position.y > transform.position.y)
                    Clamp.SetMapBounds(BeforeId);
                break;
            case SwitchDirection.BottomToTop:
                if (prevPlayerY <= transform.position.y && Player.position.y > transform.position.y)
                    Clamp.SetMapBounds(AfterId);
                else if (prevPlayerY >= transform.position.y && Player.position.y < transform.position.y)
                    Clamp.SetMapBounds(BeforeId);
                break;
        }

        prevPlayerX = Player.position.x;
        prevPlayerY = Player.position.y;
    }
}
