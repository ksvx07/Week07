using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public Dictionary<int, StageScriptableObject> StageDics { get; private set; } = new();

    #region Respawn System  
    [Header("Respawn System")]
    [SerializeField] private RespawnManager respawnManager;
    // RespawnManager에 쉽게 접근할 수 있는 프로퍼티
    public RespawnManager RespawnManager => respawnManager;
    #endregion

    private bool _isDestroyManager;

    private void Awake()
    {
        if (null == Instance)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        LoadScriptableObject("Stages");

        Initialize();
    }

    public static void Initialize()
    {
  
    }

    public void OnApplicationQuit()
    {
        DestroyManager();
    }

    private void DestroyManager()
    {
        if (_isDestroyManager) return;

        _isDestroyManager = true;
        Release();
    }

    private void Release()
    {

    }

    #region Stage System
    private void LoadScriptableObject(string path)
    {
        var stages = Resources.LoadAll<StageScriptableObject>(path);

        StageDics.Clear();

        foreach (var stage in stages)
        {
            StageDics.Add(stage.Id, stage);
        }
    }
    #endregion


   #region Respawn System Integration
    private void InitializeRespawnManager()
    {
        // RespawnManager가 할당되지 않았다면 자동으로 찾거나 생성
        if (respawnManager == null)
        {
            respawnManager = FindFirstObjectByType<RespawnManager>();
            
            if (respawnManager == null)
            {
                // RespawnManager가 없다면 새로 생성
                GameObject respawnManagerGO = new GameObject("RespawnManager");
                respawnManager = respawnManagerGO.AddComponent<RespawnManager>();
                Debug.Log("[GameManager] RespawnManager가 자동으로 생성되었습니다.");
            }
        }
    }

    // 편의 메서드들 - GameManager를 통해 RespawnManager 기능에 쉽게 접근
    public void RegisterCheckpoint(int checkpointId, Vector3 position)
    {
        if (respawnManager != null)
        {
            respawnManager.RegisterCheckpoint(checkpointId, position);
        }
    }

    public void ActivateCheckpoint(int checkpointId)
    {
        if (respawnManager != null)
        {
            respawnManager.ActivateCheckpoint(checkpointId);
        }
    }

    public void RespawnPlayer()
    {
        if (respawnManager != null)
        {
            respawnManager.PlayerDead();
        }
    }

    public Vector3 GetCurrentSpawnPosition()
    {
        return respawnManager != null ? respawnManager.GetCurrentSpawnPosition() : Vector3.zero;
    }

    public int GetCurrentCheckpointId()
    {
        return respawnManager != null ? respawnManager.GetCurrentCheckpointId() : 0;
    }
    #endregion
}
