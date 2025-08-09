using System;
using System.Threading.Tasks;

public sealed class SpinRewardService
{
    readonly IWalletRepository _repo;
    readonly ICooldownPolicy _policy;

    public SpinRewardService(IWalletRepository repo, ICooldownPolicy policy)
    {
        _repo = repo;
        _policy = policy;
    }

    public Task GrantAsync(string uid, string currencyKey, long amountUnits, int landedIndex)
    {
        var spinId = Guid.NewGuid().ToString("N"); 
        return _repo.ApplyRewardAsync(uid, currencyKey, amountUnits, landedIndex, _policy, spinId);
    }
}