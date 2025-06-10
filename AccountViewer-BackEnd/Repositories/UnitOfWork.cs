using AccountsViewer.Data;
using AccountsViewer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace AccountsViewer.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AppDbContext _context;
        private IDbContextTransaction? _transaction;

        public IAccountRepository Accounts { get; }
        public IMonthlyBalanceRepository MonthlyBalances { get; }
        public IUploadAuditRepository UploadAudits { get; }

        public UnitOfWork(
            AppDbContext context,
            IAccountRepository accounts,
            IMonthlyBalanceRepository monthlyBalances,
            IUploadAuditRepository uploadAudits)
        {
            _context = context;
            Accounts = accounts;
            MonthlyBalances = monthlyBalances;
            UploadAudits = uploadAudits;
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitAsync()
        {
            await _transaction.CommitAsync();
        }

        public async Task RollbackAsync()
        {
            await _transaction.RollbackAsync();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}
