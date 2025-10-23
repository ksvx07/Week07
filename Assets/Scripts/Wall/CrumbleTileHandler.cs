using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

/// <summary>
/// CrumbleTileHandler - 떨림 효과 추가 버전
/// 1. 플레이어 접촉 시 좌우로 떨리는 효과
/// 2. 간단한 스케일 축소 효과
/// 3. 부숴져서 사라지는 조각 효과 추가
/// 4. 타일 생성 직전 0.1초 추가 대기로 안정성 강화
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
    private Coroutine shakeCoroutine;

    [Header("이펙트 설정")]
    [SerializeField] private GameObject crumbleEffectPrefab;
    [SerializeField] private GameObject brokenTilePrefab;

    [Header("떨림 효과 설정")]
    [SerializeField] private float shakeIntensity = 0.1f;  // 떨림 강도
    [SerializeField] private float shakeSpeed = 30f;       // 떨림 속도 (Hz)

    [Header("축소 효과 설정")]
    [SerializeField] private float scaleDuration = 0.3f;

    [Header("조각 효과 설정")]
    [SerializeField] private int brokenTileCount = 5;
    [SerializeField] private float brokenTileForce = 5f;
    [SerializeField] private float brokenTileLifetime = 2f;

    [Header("리스폰 대기 설정")]
    [SerializeField] private float respawnCheckInterval = 0.1f;
    [SerializeField] private float maxRespawnWaitTime = 10f;
    [SerializeField] private float finalSafetyDelay = 0.1f;

    // 떨림 효과용 변수
    private GameObject shakingSprite;
    private Vector3 originalTilePosition;
    private Color originalTileColor;

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

    /// <summary>
    /// BrokenTilePrefab 설정 메서드
    /// </summary>
    public void SetBrokenTilePrefab(GameObject prefab)
    {
        brokenTilePrefab = prefab;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDestroyed || isRespawning)
            return;

        if (!collision.CompareTag("Player"))
            return;

        if (currentCoroutine != null)
            return;

        // ✅ 떨림 효과 시작
        if (shakeCoroutine == null)
        {
            shakeCoroutine = StartCoroutine(ShakeEffect());
        }

        currentCoroutine = StartCoroutine(CrumbleSequence());
    }

    /// <summary>
    /// ✅ 새로 추가: 타일 떨림 효과
    /// 실제 타일맵 위에 스프라이트를 오버레이하고 흔들기
    /// </summary>
    private IEnumerator ShakeEffect()
    {
        // 1. 타일의 스프라이트 가져오기
        Sprite tileSprite = GetTileSpriteAtPosition();
        if (tileSprite == null)
        {
            Debug.LogWarning("[CrumbleTile] 타일 스프라이트를 찾을 수 없습니다.");
            yield break;
        }

        // 2. 떨림용 임시 GameObject 생성
        Vector3 worldPos = tilemap.GetCellCenterWorld(gridPos);
        shakingSprite = new GameObject($"ShakingTile_{gridPos.x}_{gridPos.y}");
        shakingSprite.transform.position = worldPos;
        shakingSprite.transform.SetParent(transform);

        // 3. SpriteRenderer 추가
        SpriteRenderer sr = shakingSprite.AddComponent<SpriteRenderer>();
        sr.sprite = tileSprite;
        sr.sortingLayerName = tilemap.GetComponent<TilemapRenderer>().sortingLayerName;
        sr.sortingOrder = tilemap.GetComponent<TilemapRenderer>().sortingOrder;

        // 4. 원본 타일을 투명하게 만들기 (떨리는 스프라이트만 보이도록)
        originalTileColor = tilemap.GetColor(gridPos);
        tilemap.SetColor(gridPos, new Color(1, 1, 1, 0));

        // 5. destroyDelay 동안 좌우로 떨기
        float elapsedTime = 0f;
        originalTilePosition = worldPos;

        while (elapsedTime < destroyDelay)
        {
            float offset = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
            shakingSprite.transform.position = originalTilePosition + new Vector3(offset, 0, 0);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 6. 떨림 종료 - 원래 위치로 복귀
        shakingSprite.transform.position = originalTilePosition;

        shakeCoroutine = null;
    }

    /// <summary>
    /// 타일맵에서 특정 위치의 스프라이트 가져오기
    /// </summary>
    private Sprite GetTileSpriteAtPosition()
    {
        TileBase tile = tilemap.GetTile(gridPos);
        if (tile == null) return null;

        // Tile 타입이면 sprite 속성 사용
        if (tile is Tile standardTile)
        {
            return standardTile.sprite;
        }

        // RuleTile 등 다른 타입의 경우 리플렉션 사용
        var spriteProperty = tile.GetType().GetProperty("sprite");
        if (spriteProperty != null)
        {
            return spriteProperty.GetValue(tile) as Sprite;
        }

        return null;
    }

    private IEnumerator CrumbleSequence()
    {
        yield return new WaitForSeconds(destroyDelay);

        // 떨림 효과가 아직 실행 중이면 중지
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        // 떨림용 스프라이트 제거
        if (shakingSprite != null)
        {
            Destroy(shakingSprite);
        }

        // 원본 타일 색상 복원 (곧 사라질 거지만)
        tilemap.SetColor(gridPos, originalTileColor);

        // 간단한 축소 효과
        yield return StartCoroutine(ScaleAndDisappear());

        // 부서짐 이펙트 재생
        PlayCrumbleEffect();
        
        // 조각난 타일 효과 생성
        PlayBrokenTileEffect();
        
        isDestroyed = true;
        tilemap.SetTile(gridPos, null);

        yield return new WaitForSeconds(respawnDelay);

        yield return StartCoroutine(RespawnSequence());

        currentCoroutine = null;
    }

    /// <summary>
    /// 간단한 스케일 축소 효과
    /// </summary>
    private IEnumerator ScaleAndDisappear()
    {
        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        // 스케일을 1 → 0으로 축소
        while (elapsed < scaleDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / scaleDuration;
            
            // Ease-in 효과 (Mathf.Pow로 비선형 감소)
            float easeProgress = Mathf.Pow(progress, 2f);
            transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, easeProgress);
            
            yield return null;
        }

        // 원래 상태로 복구
        transform.position = originalPos;
        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// 부숴져서 사라지는 조각 효과
    /// </summary>
    private void PlayBrokenTileEffect()
    {
        Vector3 worldPos = tilemap.GetCellCenterWorld(gridPos);

        if (brokenTilePrefab != null)
        {
            // 여러 개의 조각 생성
            for (int i = 0; i < brokenTileCount; i++)
            {
                // 랜덤 위치에서 생성
                Vector3 randomOffset = new Vector3(
                    Random.Range(-0.3f, 0.3f),
                    Random.Range(-0.3f, 0.3f),
                    0
                );
                Vector3 spawnPos = worldPos + randomOffset;

                GameObject brokenTile = Instantiate(brokenTilePrefab, spawnPos, Quaternion.identity);

                // 랜덤 방향으로 물리 힘 적용
                Rigidbody2D rb = brokenTile.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    Vector2 forceDirection = Random.insideUnitCircle.normalized;
                    rb.linearVelocity = forceDirection * brokenTileForce;
                    rb.angularVelocity = Random.Range(-360f, 360f);
                }

                // 일정 시간 후 자동 제거
                Destroy(brokenTile, brokenTileLifetime);
            }
        }
        else
        {
            Debug.LogWarning("[CrumbleTile] brokenTilePrefab이 할당되지 않았습니다!");
        }
    }

    /// <summary>
    /// 리스폰 직전 플레이어 충돌 검사
    /// 플레이어가 위에 있으면 생성을 지연시킴
    /// </summary>
    private IEnumerator RespawnSequence()
    {
        isRespawning = true;
        float totalWaitTime = 0f;

        // 1️⃣ 플레이어가 타일에서 떨어질 때까지 기다림
        while (IsEntityOnTile() && totalWaitTime < maxRespawnWaitTime)
        {
            yield return new WaitForSeconds(respawnCheckInterval);
            totalWaitTime += respawnCheckInterval;
        }

        // 2️⃣ 최대 대기 시간 체크
        if (totalWaitTime >= maxRespawnWaitTime)
        {
            Debug.LogWarning($"[CrumbleTile] 최대 대기 시간({maxRespawnWaitTime}초) 초과, 강제로 타일 복구");
        }

        // 3️⃣ 최종 안전 지연
        yield return new WaitForSeconds(finalSafetyDelay);

        // 4️⃣ 마지막으로 한 번 더 확인 - 아직 플레이어가 있으면 재귀 호출
        if (IsEntityOnTile())
        {
            isRespawning = false;
            yield return StartCoroutine(RespawnSequence());
            yield break;
        }

        // 5️⃣ 타일 즉시 복구
        tilemap.SetTile(gridPos, crumbleTile);
        tilemap.SetColor(gridPos, Color.white);  // 색상도 복구

        yield return new WaitForSeconds(fadeDuration);

        // 6️⃣ 리스폰 완료
        isDestroyed = false;
        isRespawning = false;
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

            if (hit.CompareTag("Player"))
            {
                return true;
            }
        }

        return false;
    }

    private void OnDestroy()
    {
        if (currentCoroutine != null)
            StopCoroutine(currentCoroutine);

        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        if (shakingSprite != null)
            Destroy(shakingSprite);
    }
}