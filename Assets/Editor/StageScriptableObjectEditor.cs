using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(StageScriptableObject))]
public class StageScriptableObjectEditor : Editor
{
    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUIX;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUIX;
    }

    private void OnSceneGUIX(SceneView sceneView)
    {
        StageScriptableObject stage = (StageScriptableObject)target;
        if (stage == null) return;

        // 중심과 크기 계산
        Vector3 center = new Vector3(
            (stage.minX + stage.maxX) / 2f,
            (stage.minY + stage.maxY) / 2f,
            0
        );

        Vector3 size = new Vector3(
            Mathf.Abs(stage.maxX - stage.minX),
            Mathf.Abs(stage.maxY - stage.minY),
            0
        );

        // 박스 그리기
        Handles.color = Color.green;
        Handles.DrawWireCube(center, size);

        // 중앙 핸들 드래그
        EditorGUI.BeginChangeCheck();
        Vector3 newCenter = Handles.PositionHandle(center, Quaternion.identity);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(stage, "Move Stage Area");

            float dx = newCenter.x - center.x;
            float dy = newCenter.y - center.y;
            stage.minX += dx; stage.maxX += dx;
            stage.minY += dy; stage.maxY += dy;

            EditorUtility.SetDirty(stage);
            // 변경 사항 즉시 저장하려면 필요 시:
            // AssetDatabase.SaveAssets();
        }
    }
}
