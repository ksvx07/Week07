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

    [SerializeField] private int _defaultStageId = 1;
    private float _targetMinX, _targetMinY, _targetMaxX, _targetMaxY;

    private void Start()
    {
        SetMapBounds(_defaultStageId);
        SetInitMapBounds();
    }

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

        float clampX = Mathf.Clamp(desiredPos.x, _minX +  camWidth, _maxX - camWidth);
        float clampY = Mathf.Clamp(desiredPos.y, _minY + camHeight, _maxY - camHeight);

        return new Vector3(clampX, clampY, desiredPos.z);
    }

    public void SetMapBounds(int Id)
    {
        if(GameManager.Instance.StageDics.TryGetValue(Id, out var mapDefinition))
        {
            _targetMinX = mapDefinition.minX;
            _targetMaxX = mapDefinition.maxX;
            _targetMinY = mapDefinition.minY;
            _targetMaxY = mapDefinition.maxY;
        }
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

    private void SetInitMapBounds()
    {
        if (GameManager.Instance.StageDics.TryGetValue(_defaultStageId, out var mapDefinition))
        {
            _minX = mapDefinition.minX;
            _maxX = mapDefinition.maxX;
            _minY = mapDefinition.minY;
            _maxY = mapDefinition.maxY;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 center = new Vector3((_minX + _maxX) / 2f, (_minY + _maxY) / 2f, 0);
        Vector3 size = new Vector3(_maxX - _minX, _maxY - _minY, 0);

        Gizmos.DrawWireCube(center, size);
    }
}
