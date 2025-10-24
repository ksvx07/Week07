using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private int checkpointId;

    private void Start()
    {
        RespawnManager.Instance.RegisterCheckpoint(checkpointId, transform.position);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            RespawnManager.Instance.ActivateCheckpoint(checkpointId);
        }
    }
}