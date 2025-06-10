using AccountsViewer.Data;
using AccountsViewer.Entities;
using AccountsViewer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AccountsViewer.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly AppDbContext _db;

        public AccountRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<Account> GetByNameAsync(string accountName)
        {
            accountName = accountName.Replace("’", "'").Trim();
            return await _db.Accounts
                .FirstAsync(a => a.Name == accountName);
        }

        public async Task<IList<Account>> GetAllAsync()
        {
            return await _db.Accounts.ToListAsync();
        }
    }
}
