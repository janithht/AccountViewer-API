using AccountsViewer.Entities;

namespace AccountsViewer.Repositories.Interfaces
{
    public interface IAccountRepository
    {
        Task<Account> GetByNameAsync(string accountName);
        Task<IList<Account>> GetAllAsync();
    }
}
