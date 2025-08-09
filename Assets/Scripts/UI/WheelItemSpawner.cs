using System.Collections.Generic;
using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class WheelItemSpawner : MonoBehaviour
{
    public RectTransform _itemPrefab;      
    public int _itemCount = 4;        
    public float _radius = 30f;          
    public float _startAngle = 22.5f;      

    public List<string> values = new List<string>()
    {
        "10","50","100","200"
    };

    public bool rotateWithAngle = true;  
    public float rotationOffset = 0f;    

    public void GenerateItems()
    {
        if (_itemPrefab == null) return;
        if (values.Count < _itemCount) _itemCount = values.Count;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c != _itemPrefab)
            {
#if UNITY_EDITOR
                DestroyImmediate(c.gameObject);
#else
                Destroy(c.gameObject);
#endif
            }
        }

        for (int i = 0; i < _itemCount; i++)
        {
            float angle = _startAngle + (360f / _itemCount) * i;
            float rad = angle * Mathf.Deg2Rad;

            var inst = Instantiate(_itemPrefab, transform);
            inst.name = $"Gem_{i:D2}";
            inst.anchoredPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * _radius;

            if (rotateWithAngle)
                inst.localRotation = Quaternion.Euler(0, 0, angle + rotationOffset);
            else
                inst.localRotation = Quaternion.Euler(0, 0, rotationOffset);

            var txt = inst.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
                txt.text = values[i];
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_itemPrefab != null && _itemPrefab.parent == transform)
            GenerateItems();
    }
#endif
}