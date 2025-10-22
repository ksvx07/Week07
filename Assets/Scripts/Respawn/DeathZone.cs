using UnityEngine;

public class DeathZone : MonoBehaviour
{
    #region 상태변수
    [Header("Death Zone Settings")]
    [SerializeField] private float respawnDelay = 0.5f;
    #endregion
    
    #region 플레이어 감지
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 플레이어가 아니면 무시
        if (!other.CompareTag("Player")) return;

        // GameManager 인스턴스 존재 여부 확인
        if (GameManager.Instance == null)
        {
            Debug.LogError("[DeathZone] GameManager instance not found!");
            return;
        }

        Debug.Log($"[DeathZone] Player entered death zone at {transform.position}");

        // 플레이어 리스폰
        GameManager.Instance.RespawnPlayer();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 플레이어가 아니면 무시
        if (!collision.gameObject.CompareTag("Player")) return;

        // GameManager 인스턴스 존재 여부 확인
        if (GameManager.Instance == null)
        {
            Debug.LogError("[DeathZone] GameManager instance not found!");
            return;
        }

        Debug.Log($"[DeathZone] Player entered death zone at {transform.position}");

        if(transform.parent.name == "Spike")
        {
            if(collision.gameObject.name == "Triangle")
            {
                return;
            }
        }
        // 플레이어 리스폰
        GameManager.Instance.RespawnPlayer();
    }
    #endregion

    #region Debug Methods
    /// <summary>
    /// 에디터에서 데스존 영역을 시각적으로 표시
    /// </summary>
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        // Collider2D 컴포넌트 가져오기
        var collider2D = GetComponent<Collider2D>();
        if (collider2D != null)
        {
            // BoxCollider2D인 경우
            if (collider2D is BoxCollider2D boxCollider)
            {
                Gizmos.DrawWireCube(transform.position + (Vector3)boxCollider.offset, boxCollider.size);
            }
            // CircleCollider2D인 경우
            else if (collider2D is CircleCollider2D circleCollider)
            {
                Gizmos.DrawWireSphere(transform.position + (Vector3)circleCollider.offset, circleCollider.radius);
            }
        }
    }
}
    #endregion
