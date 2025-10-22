using UnityEngine;

[CreateAssetMenu(fileName = "Stage", menuName = "Game/Stage ScriptableObject")]
public class StageScriptableObject : ScriptableObject
{
    public int Id;
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;
}
