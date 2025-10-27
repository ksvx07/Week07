using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LowLevel;

public class StageManager : MonoBehaviour
{
    public PlayerDataLog playerLog; // Inspector에서 할당하거나 FindObjectOfType으로 찾기

    public static StageManager Instance;

    // 이제 이 리스트에 직접 StageScriptableObject 파일을 드래그 앤 드롭하여 사용합니다.
    public List<StageScriptableObject> stages;

    public StageScriptableObject CurrentStageData { get; private set; }
    public int CurrentStageIndex { get; private set; }

    [SerializeField] private CameraClamp cameraClamp;

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

        InitializeStageData();
    }

    private void InitializeStageData()
    {
        if (stages != null && stages.Count > 0)
        {
            // 0번 인덱스로 첫 스테이지 설정
            SetStage(0);
        }
        else
        {
            Debug.LogError("StageManager에 할당된 stage가 없습니다!");
        }
    }

    /// <summary>
    /// 지정된 인덱스로 현재 스테이지를 변경하고 이벤트를 호출하는 통합 메서드
    /// </summary>
    /// <param name="index">변경할 스테이지의 인덱스</param>
    private void SetStage(int index)
    {
        // 인덱스 유효성 검사
        if (index < 0 || index >= stages.Count)
        {
            Debug.LogWarning($"요청한 스테이지 인덱스({index})가 유효한 범위를 벗어났습니다.");
            return;
        }

        CurrentStageIndex = index;
        CurrentStageData = stages[CurrentStageIndex];
        cameraClamp.SetMapBounds(CurrentStageData);

        CheckShapeStageLock(); // 스테이지에 맞게 도형 잠금
        Debug.Log($"Stage {CurrentStageIndex + 1} 로 변경되었습니다.");
        playerLog.OnEnterStage(CurrentStageData.name);
    }

    /// <summary>
    /// 특정 스테이지를 '목표 지점'으로 삼아 이동하거나, 그 목표의 '바로 이전' 스테이지로 이동 시키는 함수입니다.
    /// </summary>
    /// <param name="baseStage">이동의 기준점이 될 스테이지 데이터</param>
    /// <param name="goToNext">.
    /// - true: 'baseStage' 자체로 이동
    /// - false: 'baseStage'의 바로 이전 스테이지로 이동
    /// </param>
    public void RequestStageChange(StageScriptableObject baseStage, bool goToNext = true)
    {
        int currentIndex = stages.IndexOf(baseStage);

        if (currentIndex == -1)
        {
            Debug.LogError($"{baseStage.name}이 StageManager의 리스트에 없습니다!", baseStage);
            return;
        }

        int targetIndex = goToNext ? currentIndex : currentIndex - 1;

        SetStage(targetIndex);
    }

    private void CheckShapeStageLock()
    {

        if (CurrentStageData.isShapeLockStage)
        {
            ShapeUnlockSystem.Initialize(CurrentStageData.initailizeShape);
            return;
        }

        if (CurrentStageData.unlockAll)
        {
            ShapeUnlockSystem.UnLockAllShape();
            unlockAll = true;
        }

    }
    public bool unlockAll = false;
}
