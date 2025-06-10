using AccountsViewer.DTOs;
using AccountsViewer.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AccountsViewer.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "User")]
    public class BalanceController : ControllerBase
    {
        private readonly IBalanceService _balanceService;

        public BalanceController(IBalanceService balanceService)
        {
            _balanceService = balanceService;
        }

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatest()
        {
            LatestBalancesDto dto = await _balanceService.GetLatestBalancesAsync();
            return Ok(dto);
        }
    }
}
