using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using DG.Tweening;

/// <summary>
/// CrumbleTileHandler 고급 버전
/// - 개별 타일의 알파값을 제어합니다
/// - 타일 제거 시 시각적 이펙트를 추가할 수 있습니다
/// 
/// 주의: 이 버전은 Scriptable Tile이 필요하거나
/// 타일맵 전체가 아닌 개별 타일 렌더링이 필요합니다
/// </summary>
public class CrumbleTileHandler : MonoBehaviour
{
    private Vector3Int gridPos;
    private Tilemap tilemap;
    private TileBase crumbleTile;
    private float destroyDelay;
    private float respawnDelay;
    private float fadeDuration;

    private bool isDestroyed = false;
    private bool isRespawning = false;
    private Coroutine currentCoroutine;

    // 이펙트용 프리팹 (선택사항)
    [SerializeField] private GameObject crumbleEffectPrefab;

    // 떨림 효과 설정
    [Header("떨림 효과 설정")]
    [SerializeField] private float shakeStrength = 0.2f;  // 떨림 강도
    [SerializeField] private float shakeDuration = 0.6f;  // 떨림 지속 시간

    public void Initialize(
        Vector3Int gridPos,
        Tilemap tilemap,
        TileBase crumbleTile,
        float destroyDelay,
        float respawnDelay,
        float fadeDuration)
    {
        this.gridPos = gridPos;
        this.tilemap = tilemap;
        this.crumbleTile = crumbleTile;
        this.destroyDelay = destroyDelay;
        this.respawnDelay = respawnDelay;
        this.fadeDuration = fadeDuration;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDestroyed || isRespawning)
            return;

        // ✅ Player 태그만 감지
        if (!collision.CompareTag("Player"))
            return;

        // ✅ 이미 카운트다운이 시작됐으면 다시 시작하지 않음
        if (currentCoroutine != null)
            return;

        currentCoroutine = StartCoroutine(CrumbleSequence());
    }

    private IEnumerator CrumbleSequence()
    {
        yield return new WaitForSeconds(destroyDelay);

        // ✅ 떨림 효과 시작
        yield return StartCoroutine(ShakeAndDisappear());

        // 부서짐 이펙트 재생
        PlayCrumbleEffect();

        isDestroyed = true;
        tilemap.SetTile(gridPos, null);

        yield return new WaitForSeconds(respawnDelay);

        yield return StartCoroutine(RespawnSequence());
    }

    /// <summary>
    /// 상하좌우로 떨리다가 사라지는 효과
    /// </summary>
    private IEnumerator ShakeAndDisappear()
    {
        Vector3 originalPos = transform.position;

        // ✅ 상하좌우로 흔드는 효과 (Shake)
        transform.DOShakePosition(
            duration: shakeDuration,
            strength: new Vector3(shakeStrength, shakeStrength, 0),
            vibrato: 20,  // 떨림 횟수 (높을수록 더 빨리 떨림)
            randomness: 0.5f
        ).SetEase(Ease.InOutQuad);

        yield return new WaitForSeconds(shakeDuration);

        // ✅ 떨림 후 사라지는 효과 (스케일 축소)
        transform.DOScale(Vector3.zero, 0.3f)
            .SetEase(Ease.InBack);

        yield return new WaitForSeconds(0.3f);

        // 원래 상태로 복구 (리스폰을 위해)
        transform.position = originalPos;
        transform.localScale = Vector3.one;
    }

    private IEnumerator RespawnSequence()
    {
        isRespawning = true;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            SetTileAlpha(alpha);

            if (IsEntityOnTile())
            {
                SetTileAlpha(0f);
                isRespawning = false;

                yield return new WaitForSeconds(destroyDelay);
                yield return StartCoroutine(RespawnSequence());
                yield break;
            }

            yield return null;
        }

        SetTileAlpha(1f);
        tilemap.SetTile(gridPos, crumbleTile);
        isDestroyed = false;
        isRespawning = false;
    }

    private void SetTileAlpha(float alpha)
    {
        TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
        if (renderer != null && renderer.material != null)
        {
            Color color = renderer.material.color;
            color.a = alpha;
            renderer.material.color = color;
        }
    }

    private void PlayCrumbleEffect()
    {
        if (crumbleEffectPrefab != null)
        {
            Vector3 worldPos = tilemap.GetCellCenterWorld(gridPos);
            Instantiate(crumbleEffectPrefab, worldPos, Quaternion.identity);
        }
    }

    private bool IsEntityOnTile()
    {
        Vector3 worldPos = tilemap.GetCellCenterWorld(gridPos);
        Vector3 tileSize = tilemap.cellSize;

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            worldPos,
            tileSize * 0.9f,
            0f
        );

        BoxCollider2D myCollider = GetComponent<BoxCollider2D>();

        foreach (var hit in hits)
        {
            if (hit == myCollider)
                continue;

            if (hit.isTrigger)
                continue;

            // "Player" 태그만 감지
            if (hit.CompareTag("Player"))
            {
                return true;
            }
        }

        return false;
    }

    private void OnDestroy()
    {
        // ✅ 오브젝트가 파괴될 때 진행 중인 트윈 모두 정지 (메모리 누수 방지)
        transform.DOKill();
    }
}