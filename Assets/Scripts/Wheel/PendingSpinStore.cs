using System;
using UnityEngine;

public enum PendingStage { Created, Decided } 

[Serializable]
public struct PendingSpin
{
    public string SpinId;
    public string CurrencyKey;  
    public long AmountUnits;   
    public int Index;        
    public PendingStage Stage; 
}

public static class PendingSpinStore
{
    const string Key = "pending_spin_v1";

    public static void Save(PendingSpin p)
    {
        PlayerPrefs.SetString(Key, JsonUtility.ToJson(p));
        PlayerPrefs.Save();
    }

    public static bool TryLoad(out PendingSpin p)
    {
        if (!PlayerPrefs.HasKey(Key)) { p = default; return false; }
        p = JsonUtility.FromJson<PendingSpin>(PlayerPrefs.GetString(Key));
        return !string.IsNullOrEmpty(p.SpinId);
    }

    public static void Clear() => PlayerPrefs.DeleteKey(Key);
}