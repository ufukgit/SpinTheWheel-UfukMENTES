using System;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class WheelRewards : MonoBehaviour
{
    [SerializeField] AwardSlot[] _slots;  

    public int Count => _slots != null ? _slots.Length : 0;

    public AwardSlot GetSlotByIndex(int index)
    {
        if (_slots == null || _slots.Length == 0) return null;
        int i = Mathf.Abs(index) % _slots.Length;
        return _slots[i];
    }

    void OnValidate()
    {
        _slots = GetComponentsInChildren<AwardSlot>(true);
        Array.Sort(_slots, (a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
    }
}