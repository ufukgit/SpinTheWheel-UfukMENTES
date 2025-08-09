using UnityEngine;


[DisallowMultipleComponent]
public class AwardSlot : MonoBehaviour
{
    public RewardCurrency Currency = RewardCurrency.Money;

    public long Amount = 0;

    public int Scale = 1;

    public string CurrencyKey => Currency == RewardCurrency.Money ? "Money" : "Gem";

    public string GetDisplayText()
    {
        if (Scale > 1) return (Amount / (float)Scale).ToString("0.##");
        return Amount.ToString();
    }
}

public enum RewardCurrency { Money, Gem }

public static class RewardCurrencyUtil
{
    public static string Key(RewardCurrency c) => c.ToString(); // "Money", "Gem", ...
}