using System.Collections.Generic;
using UnityEngine;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance;

    // 이제 이 리스트에 직접 StageScriptableObject 파일을 드래그 앤 드롭하여 사용합니다.
    public List<StageScriptableObject> stages;

    public StageScriptableObject stageData { get; private set; }
    public int currentStageID { get; private set; }

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

        stageData = stages[0];
        // 현재 스테이지 데이터 카메라 정보값으로 초기화
        cameraClamp.SetMapBounds(stageData);
    }

}
 