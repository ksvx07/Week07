using UnityEngine;
using System.Collections.Generic;

public class RespawnManager : MonoBehaviour
{
    public PlayerDataLog playerLog; // Inspector에서 할당하거나 FindObjectOfType으로 찾기

    #region Singleton
    public static RespawnManager Instance;
    #endregion

    #region Checkpoint System
    [Header("Checkpoint System")]
    [SerializeField] private Transform defaultSpawn;
    [SerializeField] private float respawnTime;
    [SerializeField] private GameObject playerParticleEffect;
    [SerializeField] private GameObject checkPointparticleEffect;


    private Transform player => PlayerManager.Instance?._currentPlayerPrefab?.transform;
    private int currentCheckpointId = 0;
    private Vector3 currentSpawnPosition;
    private Dictionary<int, Vector3> checkpoints = new Dictionary<int, Vector3>();
    #endregion

    #region Events
    public System.Action<Vector3> OnPlayerSpawned;
    public System.Action<int, Vector3> OnCheckpointReached;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        InitializeSingleton();
        InitializeCheckpointSystem();
    }
    #endregion

    #region Singleton Methods
    private void InitializeSingleton()
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
    }
    #endregion

    #region Checkpoint System Methods
    private void InitializeCheckpointSystem()
    {
        currentSpawnPosition = defaultSpawn.position;
        checkpoints[0] = defaultSpawn.position;
        SpawnPlayerAtCheckpoint();
    }

    public void RegisterCheckpoint(int checkpointId, Vector3 position)
    {
        checkpoints[checkpointId] = position;
    }

    public void ActivateCheckpoint(int checkpointId)
    {
        if (!checkpoints.ContainsKey(checkpointId))
        {
            return;
        }

        if (checkpointId != currentCheckpointId)
        {
            currentCheckpointId = checkpointId;
            currentSpawnPosition = checkpoints[checkpointId];
            OnCheckpointReached?.Invoke(checkpointId, currentSpawnPosition);

            playerLog.OnReachCheckpoint(checkpointId.ToString());

            if (checkPointparticleEffect != null && ValidatePlayer())
            {
                Instantiate(checkPointparticleEffect, player.position, Quaternion.identity);
            }
        }

    }

    public void PlayerDead()
    {
        if (!ValidatePlayer()) return;
        PlayerManager.Instance.PlayerSetActive(false);
        PlayerManager.Instance.OnPlayerDead();
        PlayerManager.Instance.SetCanChangeTimeScale(false);
        PlayerManager.Instance.OriginalTimeScaleImmediate();
        if (playerParticleEffect != null)
        {
            Instantiate(playerParticleEffect, player.position, Quaternion.identity);
        }
        Invoke("RespawnPlayer", respawnTime);
    }

    private void RespawnPlayer()
    {
        PlayerManager.Instance.SetCanChangeTimeScale(true);
        ResetPlayerPhysics();
        SpawnPlayerAtCheckpoint();
        PlayerManager.Instance.PlayerSetActive(true);
    }

    private bool ValidatePlayer()
    {
        if (player == null)
        {
            return false;
        }
        return true;
    }

    private void ResetPlayerPhysics()
    {
        var rigidbody2D = player.GetComponent<Rigidbody2D>();
        if (rigidbody2D != null)
        {
            rigidbody2D.linearVelocity = Vector2.zero;
            rigidbody2D.angularVelocity = 0f;
        }
    }

    private void SpawnPlayerAtCheckpoint()
    {
        if (!ValidatePlayer()) return;

        player.position = currentSpawnPosition;
        CameraController.Instance.Cam.transform.position = currentSpawnPosition + new Vector3(0, 0, -10f);
        OnPlayerSpawned?.Invoke(currentSpawnPosition);
    }

    public void ResetCheckpoints()
    {
        currentCheckpointId = 0;
        currentSpawnPosition = defaultSpawn.position;
        Debug.Log("[RespawnManager] Checkpoints reset to default");
    }
    #endregion

    #region Public Accessors
    public Vector3 GetCurrentSpawnPosition() => currentSpawnPosition;
    public int GetCurrentCheckpointId() => currentCheckpointId;
    public int GetCheckpointCount() => checkpoints.Count;
    #endregion
}