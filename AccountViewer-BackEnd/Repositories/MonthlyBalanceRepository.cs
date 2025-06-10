using AccountsViewer.Data;
using AccountsViewer.Entities;
using AccountsViewer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AccountsViewer.Repositories
{
    public class MonthlyBalanceRepository : IMonthlyBalanceRepository
    {
        private readonly AppDbContext _db;

        public MonthlyBalanceRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<MonthlyBalance?> GetByAccountYearMonthAsync(int accountId, int year, int month)
        {
            return await _db.MonthlyBalances
                .FirstOrDefaultAsync(mb =>
                    mb.AccountId == accountId &&
                    mb.Year == year &&
                    mb.Month == month);
        }

        public async Task AddAsync(MonthlyBalance entity)
        {
            _db.MonthlyBalances.Add(entity);
            await _db.SaveChangesAsync();
        }

        public async Task<IList<MonthlyBalance>> GetByYearMonthAsync(int year, int month)
        {
            return await _db.MonthlyBalances
                .Include(mb => mb.Account)
                .Where(mb => mb.Year == year && mb.Month == month)
                .ToListAsync();
        }

        public async Task<(int Year, int Month)> GetLatestYearMonthAsync()
        {
            var latest = await _db.MonthlyBalances
                .OrderByDescending(mb => mb.Year)
                .ThenByDescending(mb => mb.Month)
                .Select(mb => new { mb.Year, mb.Month })
                .FirstOrDefaultAsync();

            return latest == null
                ? (0, 0)
                : (latest.Year, latest.Month);
        }
    }
}
