using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// 부서지는 벽 타일을 관리하는 매니저
/// 게임 시작 시 모든 부서지는 벽 타일을 찾아 자동으로 핸들러를 생성합니다.
/// </summary>
public class CrumbleTileManager : MonoBehaviour
{
    [Header("Tilemap 설정")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private TileBase crumbleTile; // 부서지는 벽 타일 에셋

    [Header("타임 설정")]
    [SerializeField] private float destroyDelay = 2f;     // 밟은 후 부서질 때까지의 시간
    [SerializeField] private float respawnDelay = 5f;     // 부서진 후 리스폰할 때까지의 시간
    [SerializeField] private float fadeDuration = 1f;     // 리스폰 시 알파 페이드 시간

    private Dictionary<Vector3Int, CrumbleTileHandler> tileHandlers = new();

    private void Start()
    {
        InitializeCrumbleTiles();
    }

    /// <summary>
    /// 타일맵에서 모든 부서지는 벽 타일을 찾아 핸들러를 생성합니다.
    /// </summary>
    private void InitializeCrumbleTiles()
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap이 할당되지 않았습니다!");
            return;
        }

        if (crumbleTile == null)
        {
            Debug.LogError("Crumble Tile이 할당되지 않았습니다!");
            return;
        }

        // 타일맵의 모든 위치 스캔
        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(pos);
            
            // crumbleTile과 일치하는 타일 찾기
            if (tile == crumbleTile)
            {
                CreateTileHandler(pos);
            }
        }

        Debug.Log($"부서지는 벽 타일 {tileHandlers.Count}개 생성 완료");
    }

    /// <summary>
    /// 각 부서지는 벽 타일마다 핸들러 GameObject를 생성합니다.
    /// </summary>
    private void CreateTileHandler(Vector3Int gridPos)
    {
        // 핸들러용 GameObject 생성
        GameObject handler = new GameObject($"CrumbleTile_{gridPos.x}_{gridPos.y}");
        handler.transform.SetParent(transform);

        // 월드 좌표 설정
        Vector3 worldPos = tilemap.GetCellCenterWorld(gridPos);
        handler.transform.position = worldPos;

        // Rigidbody2D 추가 (OnTriggerEnter2D가 작동하기 위해 필수)
        Rigidbody2D rb = handler.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;  // 물리 영향 없음

        // Trigger 콜라이더 추가
        BoxCollider2D collider = handler.AddComponent<BoxCollider2D>();
        collider.size = tilemap.cellSize * 0.95f;
        collider.isTrigger = true;

        // CrumbleTileHandler 컴포넌트 추가 및 초기화
        CrumbleTileHandler tileHandler = handler.AddComponent<CrumbleTileHandler>();
        tileHandler.Initialize(
            gridPos, 
            tilemap, 
            crumbleTile, 
            destroyDelay, 
            respawnDelay, 
            fadeDuration
        );

        tileHandlers[gridPos] = tileHandler;
    }
}