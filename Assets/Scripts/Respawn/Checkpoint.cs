using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private int checkpointId;
    
    private void Start()
    {
        GameManager.Instance.RegisterCheckpoint(checkpointId, transform.position);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.ActivateCheckpoint(checkpointId);
        }
    }
}