using System.Threading.Tasks;

public sealed class FirebaseRewardApplier : IRewardApplier
{
    public async Task ApplyAsync(string userId, string currencyKey, long amountUnits, int landedIndex)
    {
        if (!FirebaseServices.Instance || !FirebaseServices.Instance.OnlineMode)
            return;

        var spinService = FirebaseServices.Instance.SpinService;
        if (spinService == null) return;

        await spinService.GrantAsync(userId, currencyKey, amountUnits, landedIndex);
    }
}