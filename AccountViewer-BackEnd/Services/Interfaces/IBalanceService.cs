using AccountsViewer.DTOs;

namespace AccountsViewer.Services.Interfaces
{
    public interface IBalanceService
    {
        Task<LatestBalancesDto> GetLatestBalancesAsync();
    }
}
