using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways] 
public class WheelLightSpawner : MonoBehaviour
{
    public RectTransform _lightPrefab;   
    public int _lightCount = 8;          
    public float _radius = 100f;         
    public float _startAngle = 0f;       

    public void GenerateLights()
    {
        if (_lightPrefab == null) return;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c != _lightPrefab)
            {
#if UNITY_EDITOR
                DestroyImmediate(c.gameObject);
#else
                Destroy(c.gameObject);
#endif
            }
        }

        for (int i = 0; i < _lightCount; i++)
        {
            float angle = _startAngle + (360f / _lightCount) * i;
            float rad = angle * Mathf.Deg2Rad;

            var inst = Instantiate(_lightPrefab, transform);
            inst.name = _lightPrefab.name;
            inst.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _radius;
            inst.localRotation = Quaternion.Euler(0, 0, angle);
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_lightPrefab != null && _lightPrefab.parent == transform)
            GenerateLights();
    }
#endif
}