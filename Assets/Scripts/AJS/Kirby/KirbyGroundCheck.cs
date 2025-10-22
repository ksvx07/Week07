using UnityEngine;

public class KirbyGroundCheck : MonoBehaviour
{
    private bool onGround;

    [Header("Collider Settings")]
    [SerializeField][Tooltip("땅 체크할 Raycast 길이")] private float groundLength = 0.95f;
    [SerializeField][Tooltip("땅 체크할 두 Raycast의 거리")] private Vector3 colliderOffset;
    [Header("Layer Masks")]
    [SerializeField][Tooltip("땅 판정 Layer")] private LayerMask groundLayer;
    private void Update()
    {
        // 2개의 RayCaste를 발사해, Player가 땅 위에 있는지 파악하기
        onGround = Physics2D.Raycast(transform.position + colliderOffset, Vector2.down, groundLength, groundLayer) || Physics2D.Raycast(transform.position - colliderOffset, Vector2.down, groundLength, groundLayer);
    }
    private void OnDrawGizmos()
    {
        //Draw the ground colliders on screen for debug purposes
        if (onGround) { Gizmos.color = Color.green; } else { Gizmos.color = Color.red; }
        Gizmos.DrawLine(transform.position + colliderOffset, transform.position + colliderOffset + Vector3.down * groundLength);
        Gizmos.DrawLine(transform.position - colliderOffset, transform.position - colliderOffset + Vector3.down * groundLength);
    }

    // 바닥 여부, 보낼 공개 함수
    public bool GetOnGround() { return onGround; }
}
