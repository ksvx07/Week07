using System.Collections.Generic;
using UnityEngine;

public class CameraClamp : MonoBehaviour
{
    [SerializeField] private Camera cam;
    [SerializeField] private float SwitchSpeed;

    [SerializeField] public float _minX;
    [SerializeField] public float _maxX;
    [SerializeField] public float _minY;
    [SerializeField] public float _maxY;

    private float _targetMinX, _targetMinY, _targetMaxX, _targetMaxY;
    private float _targetZoom = 6f;
    private float _initialZoom = 9f;

    private void Update()
    {
        _minX = Mathf.Lerp(_minX, _targetMinX, Time.deltaTime * SwitchSpeed);
        _maxX = Mathf.Lerp(_maxX, _targetMaxX, Time.deltaTime * SwitchSpeed);
        _minY = Mathf.Lerp(_minY, _targetMinY, Time.deltaTime * SwitchSpeed);
        _maxY = Mathf.Lerp(_maxY, _targetMaxY, Time.deltaTime * SwitchSpeed);
    }

    public Vector3 HandleClamp(Vector3 desiredPos)
    {
        float camHeight = cam.orthographicSize;
        float camWidth = camHeight * cam.aspect;

        // 카메라 중심이 맵 경계를 벗어나지 않도록 제한
        float clampX = Mathf.Clamp(desiredPos.x, _minX + camWidth, _maxX - camWidth);
        float clampY = Mathf.Clamp(desiredPos.y, _minY + camHeight, _maxY - camHeight);

        return new Vector3(clampX, clampY, desiredPos.z);
    }

    public void SetMapBounds(StageScriptableObject stageData)
    {
        // stage Data 에 있는 값 가져오기
        _targetMinX = stageData.minX;
        _targetMaxX = stageData.maxX;
        _targetMinY = stageData.minY;
        _targetMaxY = stageData.maxY;


        var mapBoundsWidth = _targetMaxX - _targetMinX;
        float camWidth = cam.orthographicSize * 2 * cam.aspect;

        var zoom = mapBoundsWidth < camWidth ? _targetZoom : _initialZoom;

        cam.GetComponent<CameraController>().TriggerZoom(zoom);
    }

    public List<float> GetMapBounds()
    {
        var bounds = new List<float>
        {
            _minX,
            _maxX,
            _minY,
            _maxY
        };

        return bounds;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 center = new Vector3((_minX + _maxX) / 2f, (_minY + _maxY) / 2f, 0);
        Vector3 size = new Vector3(_maxX - _minX, _maxY - _minY, 0);

        Gizmos.DrawWireCube(center, size);
    }
}
