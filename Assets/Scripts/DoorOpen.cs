using UnityEngine;

public class DoorOpen : MonoBehaviour
{
    [SerializeField] private GameObject door;
    private bool opened;


    private void Start()
    {
        RespawnManager.Instance.OnPlayerSpawned += PlayerSpawned;
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Trigger Entered");
        if (collision.CompareTag("Player"))
        {
            if (opened) return;
            opened = true;

            door.SetActive(false);
            gameObject.SetActive(false);
        }
    }

    private void PlayerSpawned(Vector3 _noNeed)
    {
        if (!opened) return;
        opened = false;
        door.SetActive(true);
        gameObject.SetActive(true);
    }

}
