using UnityEngine;

[CreateAssetMenu(fileName = "Stage", menuName = "Game/Stage ScriptableObject")]
public class StageScriptableObject : ScriptableObject
{
    public int Id;
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    [Header("도형 잠금 설정")]
    public bool isShapeLockStage;
    public PlayerShape initailizeShape;

    [Header("도형 해금 설정")]
    public bool unlockAll;
}