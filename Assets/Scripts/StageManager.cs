using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    private int _currentStageId = 0;

    public int CurrentStageId
    {
        get
        {
            if (Instance == null)
            {
                Debug.LogError("StageManager instance 없음");
                return -1;
            }
            return Instance._currentStageId;
        }
        set
        {
            if (Instance == null)
            {
                Debug.LogError("StageManager instance 없음");
                return;
            }
            Instance._currentStageId = value;
        }
    }

    public bool IsTutorialStage
    {
        get
        {
            return _currentStageId == 1 || _currentStageId == 2 || _currentStageId == 3 || _currentStageId == 4;
        }
    }

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
    }
}
