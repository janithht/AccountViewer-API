using AccountsViewer.Entities;
using AccountsViewer.Repositories.Interfaces;
using AccountsViewer.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountViewer.Tests.Services
{
    public class BalanceServiceTests
    {
        private readonly Mock<IMonthlyBalanceRepository> _mockBalanceRepo;
        private readonly BalanceService _balanceService;

        public BalanceServiceTests()
        {
            _mockBalanceRepo = new Mock<IMonthlyBalanceRepository>();
            _balanceService = new BalanceService(_mockBalanceRepo.Object);
        }

        [Fact]
        public async Task GetLatestBalancesAsync_WhenNoDataExists_ReturnsEmptyResult()
        {
            _mockBalanceRepo.Setup(x => x.GetLatestYearMonthAsync())
                .ReturnsAsync((0, 0));

            var result = await _balanceService.GetLatestBalancesAsync();

            Assert.Equal(0, result.Year);
            Assert.Equal(0, result.Month);
            Assert.Empty(result.Items);
            _mockBalanceRepo.Verify(x => x.GetByYearMonthAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task GetLatestBalancesAsync_WhenDataExists_ReturnsBalances()
        {
            var testYear = 2023;
            var testMonth = 6;
            var testBalances = new List<MonthlyBalance>
            {
                new MonthlyBalance { Account = new Account { Name = "Account1" }, Balance = 1000, UploadAudit = new UploadAudit() },
                new MonthlyBalance { Account = new Account { Name = "Account2" }, Balance = 2000, UploadAudit = new UploadAudit() }
            };

            _mockBalanceRepo.Setup(x => x.GetLatestYearMonthAsync())
                .ReturnsAsync((testYear, testMonth));

            _mockBalanceRepo.Setup(x => x.GetByYearMonthAsync(testYear, testMonth))
                .ReturnsAsync(testBalances);

            var result = await _balanceService.GetLatestBalancesAsync();

            Assert.Equal(testYear, result.Year);
            Assert.Equal(testMonth, result.Month);
            Assert.Equal(2, result.Items.Count);

            var firstItem = result.Items.First();
            Assert.Equal("Account1", firstItem.AccountName);
            Assert.Equal(1000, firstItem.Balance);

            var secondItem = result.Items.Last();
            Assert.Equal("Account2", secondItem.AccountName);
            Assert.Equal(2000, secondItem.Balance);

            _mockBalanceRepo.Verify(x => x.GetByYearMonthAsync(testYear, testMonth), Times.Once);
        }

        [Fact]
        public async Task GetLatestBalancesAsync_WhenDataExistsButNoBalances_ReturnsEmptyItems()
        {
            var testYear = 2025;
            var testMonth = 6;

            _mockBalanceRepo.Setup(x => x.GetLatestYearMonthAsync())
                .ReturnsAsync((testYear, testMonth));

            _mockBalanceRepo.Setup(x => x.GetByYearMonthAsync(testYear, testMonth))
                .ReturnsAsync(new List<MonthlyBalance>());

            var result = await _balanceService.GetLatestBalancesAsync();

            Assert.Equal(testYear, result.Year);
            Assert.Equal(testMonth, result.Month);
            Assert.Empty(result.Items);
            _mockBalanceRepo.Verify(x => x.GetByYearMonthAsync(testYear, testMonth), Times.Once);
        }
    }
}
