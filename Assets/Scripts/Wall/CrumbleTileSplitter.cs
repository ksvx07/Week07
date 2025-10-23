using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

/// <summary>
/// CrumbleTileSplitter - 타일 이미지를 자동으로 분할하여 파편 효과
/// 설정 없이 자동으로 이미지를 그리드로 나누고 터뜨림
/// </summary>
public class CrumbleTileSplitter : MonoBehaviour
{
    [Header("분할 설정")]
    public int gridX = 3;  // 가로 분할 개수
    public int gridY = 3;  // 세로 분할 개수

    [Header("물리 설정")]
    public float explosionForce = 3f;
    public float upwardForce = 2f;
    public float torqueRange = 200f;

    [Header("페이드 설정")]
    public float fadeStartDelay = 0.5f;
    public float fadeSpeed = 2f;  // ✅ fadeSpeed 증가 (0.02 → 2)
    public float lifetime = 3f;

    [HideInInspector] public Sprite tileSprite;  // 외부에서 설정할 스프라이트

    private void Start()
    {
        if (tileSprite != null)
        {
            SplitAndExplode();
        }
        else
        {
            tileSprite = GetComponent<SpriteRenderer>()?.sprite;
            Destroy(gameObject);
            return;
        }

        // ✅ 분할 작업이 완료된 후 즉시 파괴 (자식들은 부모로 이동되었으므로 안전)
        Destroy(gameObject);
    }

    /// <summary>
    /// 이미지를 그리드로 나누고 터뜨림
    /// </summary>
    private void SplitAndExplode()
    {
        // ✅ 부모 오브젝트 생성 (이 오브젝트의 부모로 설정하지 않음)
        GameObject parent = new GameObject("CrumblePieces");
        parent.transform.position = transform.position;

        // 원본 스프라이트 정보
        Texture2D texture = tileSprite.texture;
        Rect spriteRect = tileSprite.rect;
        Vector2 pivot = tileSprite.pivot;

        // 픽셀 단위 계산
        float pieceWidth = spriteRect.width / gridX;
        float pieceHeight = spriteRect.height / gridY;

        // 월드 단위 크기 (PPU 고려)
        float pixelsPerUnit = tileSprite.pixelsPerUnit;
        Vector2 worldPieceSize = new Vector2(pieceWidth / pixelsPerUnit, pieceHeight / pixelsPerUnit);

        // 중심점 계산
        Vector2 spriteCenter = new Vector2(
            spriteRect.width / 2f - pivot.x,
            spriteRect.height / 2f - pivot.y
        ) / pixelsPerUnit;

        // ✅ SortingLayer 정보 가져오기 (Tilemap에서)
        string sortingLayerName = "Default";
        int sortingOrder = 0;
        
        // Tilemap Renderer에서 정보 가져오기
        Tilemap tilemap = FindFirstObjectByType<Tilemap>();
        if (tilemap != null)
        {
            TilemapRenderer tilemapRenderer = tilemap.GetComponent<TilemapRenderer>();
            if (tilemapRenderer != null)
            {
                sortingLayerName = tilemapRenderer.sortingLayerName;
                sortingOrder = tilemapRenderer.sortingOrder;
            }
        }

        // 각 조각 생성
        for (int y = 0; y < gridY; y++)
        {
            for (int x = 0; x < gridX; x++)
            {
                CreatePiece(
                    parent.transform,
                    texture,
                    spriteRect,
                    x, y,
                    pieceWidth, pieceHeight,
                    pixelsPerUnit,
                    worldPieceSize,
                    spriteCenter,
                    sortingLayerName,
                    sortingOrder
                );
            }
        }

        // 부모 오브젝트에 자동 삭제 컴포넌트 추가
        AutoDestroy autoDestroy = parent.AddComponent<AutoDestroy>();
        autoDestroy.lifetime = lifetime;
    }

    /// <summary>
    /// 개별 조각 생성
    /// </summary>
    private void CreatePiece(
        Transform parent,
        Texture2D texture,
        Rect spriteRect,
        int gridX, int gridY,
        float pieceWidth, float pieceHeight,
        float pixelsPerUnit,
        Vector2 worldPieceSize,
        Vector2 spriteCenter,
        string sortingLayerName,
        int sortingOrder)
    {
        // 조각용 GameObject 생성
        GameObject piece = new GameObject($"Piece_{gridX}_{gridY}");
        piece.transform.SetParent(parent);

        // 픽셀 좌표 계산
        float pixelX = spriteRect.x + gridX * pieceWidth;
        float pixelY = spriteRect.y + gridY * pieceHeight;

        // 새 스프라이트 렉트 생성
        Rect pieceRect = new Rect(pixelX, pixelY, pieceWidth, pieceHeight);
        
        // 조각 스프라이트 생성
        Sprite pieceSprite = Sprite.Create(
            texture,
            pieceRect,
            new Vector2(0.5f, 0.5f),  // 중심을 피벗으로
            pixelsPerUnit
        );

        // 월드 위치 계산
        float worldX = (gridX * worldPieceSize.x) - (worldPieceSize.x * this.gridX / 2f) + (worldPieceSize.x / 2f);
        float worldY = (gridY * worldPieceSize.y) - (worldPieceSize.y * this.gridY / 2f) + (worldPieceSize.y / 2f);
        
        piece.transform.position = transform.position + new Vector3(worldX, worldY, 0);

        // SpriteRenderer 추가
        SpriteRenderer sr = piece.AddComponent<SpriteRenderer>();
        sr.sprite = pieceSprite;
        sr.sortingLayerName = sortingLayerName;
        sr.sortingOrder = sortingOrder + 1;  // ✅ 타일보다 위에 렌더링

        // Rigidbody2D 추가
        Rigidbody2D rb = piece.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        
        // 폭발 효과 적용
        Vector2 explosionDirection = (piece.transform.position - transform.position).normalized;
        explosionDirection += Vector2.up * upwardForce;
        rb.linearVelocity = explosionDirection * explosionForce;
        rb.angularVelocity = Random.Range(-torqueRange, torqueRange);

        // 페이드아웃 컴포넌트 추가
        SimpleFadeOut fadeOut = piece.AddComponent<SimpleFadeOut>();
        fadeOut.fadeStartDelay = fadeStartDelay;
        fadeOut.fadeSpeed = fadeSpeed;
    }
}

/// <summary>
/// 간단한 페이드아웃 컴포넌트
/// </summary>
public class SimpleFadeOut : MonoBehaviour
{
    [HideInInspector] public float fadeStartDelay = 0.5f;
    [HideInInspector] public float fadeSpeed = 2f;

    private SpriteRenderer spriteRenderer;
    private float timer = 0f;
    private bool startFade = false;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= fadeStartDelay)
        {
            startFade = true;
        }

        if (startFade && spriteRenderer != null)
        {
            Color color = spriteRenderer.color;
            color.a -= fadeSpeed * Time.deltaTime;  // ✅ 60fps 배수 제거
            spriteRenderer.color = color;

            if (color.a <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
}

/// <summary>
/// 자식이 모두 사라지면 부모도 삭제
/// </summary>
public class AutoDestroy : MonoBehaviour
{
    [HideInInspector] public float lifetime = 3f;
    private float timer = 0f;

    private void Update()
    {
        timer += Time.deltaTime;

        // 자식이 없거나 시간 초과시 삭제
        if (transform.childCount == 0 || timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}