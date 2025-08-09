using System.Threading.Tasks;

public interface IWalletRepository
{
    Task EnsureUserAsync(string uid);
    Task<WalletData> GetAsync(string uid);

    Task ApplyRewardAsync(string uid, string currencyKey, long amountUnits, int landedIndex,
                          ICooldownPolicy policy, string spinId);
}