using System.Threading.Tasks;

public interface IRewardApplier
{
    Task ApplyAsync(string userId, string currencyKey, long amountUnits, int landedIndex, string spinId);
}