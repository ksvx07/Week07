using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

/// <summary>
/// CrumbleTileHandler - 2D 떨림 효과 + 자동 이미지 분할 파편 효과 버전
/// 1. 플레이어 접촉 시 좌우+위아래로 떨리는 효과
/// 2. 간단한 스케일 축소 효과
/// 3. 이미지를 자동으로 잘라서 터뜨리는 조각 효과
/// 4. 타일 생성 직전 0.1초 추가 대기로 안정성 강화
/// 
/// 수정사항: 
/// - 떨림 효과 시 타일 제거 + Handler 콜라이더를 solid로 변경하여 플레이어 지탱
/// - 스프라이트를 미리 캐싱하여 파편 효과에 사용
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

    // Manager로부터 전달받을 프리팹들
    private GameObject crumbleEffectPrefab;
    private GameObject tileSplitterPrefab;

    [Header("떨림 효과 설정")]
    [SerializeField] private float shakeIntensity = 0.1f;  // 떨림 강도
    [SerializeField] private float shakeSpeed = 30f;       // 떨림 속도 (Hz)

    [Header("축소 효과 설정")]
    [SerializeField] private float scaleDuration = 0.3f;

    [Header("리스폰 대기 설정")]
    [SerializeField] private float respawnCheckInterval = 0.1f;
    [SerializeField] private float maxRespawnWaitTime = 10f;
    [SerializeField] private float finalSafetyDelay = 0.1f;

    // 떨림 효과용 변수
    private GameObject shakingSprite;
    private Vector3 originalTilePosition;
    private Sprite cachedSprite;  // ✅ 스프라이트를 미리 저장

    // 콜라이더 관리용
    private BoxCollider2D myCollider;

    /// <summary>
    /// 타일 핸들러 초기화 (Manager에서 호출)
    /// </summary>
    public void Initialize(
        Vector3Int gridPos,
        Tilemap tilemap,
        TileBase crumbleTile,
        float destroyDelay,
        float respawnDelay,
        float fadeDuration,
        GameObject crumbleEffectPrefab,
        GameObject tileSplitterPrefab)
    {
        this.gridPos = gridPos;
        this.tilemap = tilemap;
        this.crumbleTile = crumbleTile;
        this.destroyDelay = destroyDelay;
        this.respawnDelay = respawnDelay;
        this.fadeDuration = fadeDuration;
        this.crumbleEffectPrefab = crumbleEffectPrefab;
        this.tileSplitterPrefab = tileSplitterPrefab;

        myCollider = GetComponent<BoxCollider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDestroyed || isRespawning)
            return;

        if (!collision.CompareTag("Player"))
            return;

        if (currentCoroutine != null)
            return;

        GroundFall();
    }


    public void GroundFall()
    {
        // 떨림 효과 시작
        if (shakeCoroutine == null)
        {
            shakeCoroutine = StartCoroutine(ShakeEffect());
        }

        currentCoroutine = StartCoroutine(CrumbleSequence());
    }

    /// <summary>
    /// 타일 떨림 효과 (2D)
    /// 타일을 제거하고 Handler의 콜라이더를 solid로 변경하여 플레이어 지탱
    /// </summary>
    private IEnumerator ShakeEffect()
    {
        // 1. 타일의 스프라이트 가져오기 및 캐싱
        cachedSprite = GetTileSpriteAtPosition();
        if (cachedSprite == null)
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
        sr.sprite = cachedSprite;
        sr.sortingLayerName = tilemap.GetComponent<TilemapRenderer>().sortingLayerName;
        sr.sortingOrder = tilemap.GetComponent<TilemapRenderer>().sortingOrder;

        // 4. ✅ 타일 완전히 제거 (중복 렌더링 방지)
        tilemap.SetTile(gridPos, null);

        // 5. ✅ Handler의 BoxCollider2D를 solid로 변경 (플레이어를 지탱)
        if (myCollider != null)
        {
            myCollider.isTrigger = false;
        }

        // 6. destroyDelay 동안 좌우 + 위아래로 떨기 (시각적 효과만)
        float elapsedTime = 0f;
        originalTilePosition = worldPos;

        while (elapsedTime < destroyDelay)
        {
            // 좌우 흔들림 (sin 파동)
            float offsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
            // 위아래 흔들림 (cos 파동으로 다른 패턴)
            float offsetY = Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shakeIntensity;

            shakingSprite.transform.position = originalTilePosition + new Vector3(offsetX, offsetY, 0);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 7. 떨림 종료 - 원래 위치로 복귀
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
        // ✅ destroyDelay 동안 대기 (이 시간 동안 Handler 콜라이더로 플레이어 지탱)
        yield return new WaitForSeconds(destroyDelay);

        // 떨림 효과가 아직 실행 중이면 중지
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
        }

        // ✅ 콜라이더를 다시 trigger로 변경 (또는 비활성화)
        if (myCollider != null)
        {
            myCollider.enabled = false;  // 완전히 비활성화
        }

        // 떨림용 스프라이트 제거
        if (shakingSprite != null)
        {
            Destroy(shakingSprite);
        }

        // 간단한 축소 효과
        yield return StartCoroutine(ScaleAndDisappear());

        // 부서짐 이펙트 재생
        PlayCrumbleEffect();

        // ✅ 이미지를 잘라서 터뜨리는 조각 효과 생성 (캐시된 스프라이트 사용)
        PlayTileSplitterEffect();

        isDestroyed = true;

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
    /// 이미지를 자동으로 잘라서 터뜨리는 효과
    /// </summary>
    private void PlayTileSplitterEffect()
    {
        if (tileSplitterPrefab == null)
        {
            return;
        }

        // ✅ 미리 캐싱해둔 스프라이트 사용
        if (cachedSprite == null)
        {
            Debug.LogWarning("[CrumbleTile] 캐시된 타일 스프라이트가 없습니다.");
            return;
        }

        // 타일의 월드 좌표 가져오기
        Vector3 worldPos = tilemap.GetCellCenterWorld(gridPos);

        // TileSplitter 프리팹 생성
        GameObject splitterObj = Instantiate(tileSplitterPrefab, worldPos, Quaternion.identity);
        CrumbleTileSplitter splitter = splitterObj.GetComponent<CrumbleTileSplitter>();

        if (splitter != null)
        {
            // ✅ 캐시된 스프라이트를 전달
            splitter.tileSprite = cachedSprite;
        }
        else
        {
            Debug.LogError("[CrumbleTile] CrumbleTileSplitter 컴포넌트를 찾을 수 없습니다!");
            Destroy(splitterObj);
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

        // ✅ 콜라이더를 다시 trigger로 설정하고 활성화
        if (myCollider != null)
        {
            myCollider.isTrigger = true;
            myCollider.enabled = true;
        }

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