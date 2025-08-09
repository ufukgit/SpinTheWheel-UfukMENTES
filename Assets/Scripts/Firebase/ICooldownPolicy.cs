using System;

public interface ICooldownPolicy
{
    (DateTime endUtc, int minutes) ComputeNext(int nextSpinCount, DateTime nowUtc);
}

public sealed class ResetEveryTenLinearCooldown : ICooldownPolicy
{
    public (DateTime endUtc, int minutes) ComputeNext(int nextSpinCount, DateTime nowUtc)
    {
        int step = (nextSpinCount - 1) % 10;
        int minutes = 10 + 5 * step;
        return (nowUtc.AddMinutes(minutes), minutes);
    }
}