using AccountsViewer.DTOs;
using AccountsViewer.Repositories.Interfaces;
using AccountsViewer.Services.Interfaces;

namespace AccountsViewer.Services
{
    public class BalanceService : IBalanceService
    {
        private readonly IMonthlyBalanceRepository _balanceRepo;

        public BalanceService(IMonthlyBalanceRepository balanceRepo)
        {
            _balanceRepo = balanceRepo;
        }

        public async Task<LatestBalancesDto> GetLatestBalancesAsync()
        {
            var (year, month) = await _balanceRepo.GetLatestYearMonthAsync();
            if (year == 0 && month == 0)
            {
                return new LatestBalancesDto
                {
                    Year = 0,
                    Month = 0,
                    Items = new List<BalanceDto>()
                };
            }

            var balances = await _balanceRepo.GetByYearMonthAsync(year, month);
            var items = balances.Select(mb => new BalanceDto
            {
                AccountName = mb.Account.Name,
                Balance = mb.Balance
            }).ToList();

            return new LatestBalancesDto
            {
                Year = year,
                Month = month,
                Items = items
            };
        }
    }
}
