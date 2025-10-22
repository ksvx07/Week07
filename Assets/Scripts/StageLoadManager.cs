using UnityEngine;

public class StageLoadManager : MonoBehaviour
{
    [SerializeField] private GameObject[] LevelPrefabs;
    private GameObject[] LoadedLevels;
    public int currntLevel = 0;

    private void Awake()
    {
        LoadedLevels = new GameObject[LevelPrefabs.Length];
        AddAllLevels();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            currntLevel++;
            if (currntLevel >= LevelPrefabs.Length)
            {
                currntLevel = 0;
            }
        }
    }



    [ContextMenu("AddAllLevels")]
    private void AddAllLevels()
    {
        for (int i = 0; i < LevelPrefabs.Length; i++)
        {
            LoadedLevels[i] = Instantiate(LevelPrefabs[i]);
        }
    }

    [ContextMenu("RestartLevel")]
    private void RestartLevel()
    {
        if (LoadedLevels[currntLevel] != null)
        {
            Destroy(LoadedLevels[currntLevel]);
        }
        LoadedLevels[currntLevel] = Instantiate(LevelPrefabs[currntLevel]);
    }
}
