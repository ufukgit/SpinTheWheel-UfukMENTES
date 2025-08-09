using System.Threading.Tasks;

public interface IRandomIndexProvider
{
    Task<int> GetIndexAsync(int max);
}