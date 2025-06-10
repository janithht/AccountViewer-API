using AccountsViewer.Entities;

namespace AccountsViewer.Repositories.Interfaces
{
    public interface IMonthlyBalanceRepository
    {
        Task<MonthlyBalance?> GetByAccountYearMonthAsync(int accountId, int year, int month);
        Task AddAsync(MonthlyBalance entity);
        Task<IList<MonthlyBalance>> GetByYearMonthAsync(int year, int month);
        Task<(int Year, int Month)> GetLatestYearMonthAsync();
    }
}
