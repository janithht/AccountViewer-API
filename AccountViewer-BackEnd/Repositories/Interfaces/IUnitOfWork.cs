namespace AccountsViewer.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IAccountRepository Accounts { get; }
        IMonthlyBalanceRepository MonthlyBalances { get; }
        IUploadAuditRepository UploadAudits { get; }
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        Task<int> SaveChangesAsync();
    }
}
