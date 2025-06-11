using AccountsViewer.Data;
using AccountsViewer.Entities;
using AccountsViewer.Repositories;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountViewer.Tests.Repositories
{
    public class MonthlyBalanceRepositoryTests
    {
        private static AppDbContext CreateContext()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(opts);
        }

        [Fact]
        public async Task GetByAccountYearMonthAsync_ReturnsEntity_WhenExists()
        {
            await using var db = CreateContext();

            var account = new Account { AccountId = 42, Name = "Cash" };
            var audit = new UploadAudit { UploadAuditId = 99, FileName = "audit.xlsx", UploadedAt = DateTime.UtcNow, ProcessStatus = UploadProcessStatus.Success };
            db.Accounts.Add(account);
            db.UploadAudits.Add(audit);

            var mb = new MonthlyBalance
            {
                AccountId = 42,
                Account = account,
                Year = 2021,
                Month = 12,
                Balance = 123.45m,
                UploadAuditId = 99,
                UploadAudit = audit
            };
            db.MonthlyBalances.Add(mb);
            await db.SaveChangesAsync();

            var repo = new MonthlyBalanceRepository(db);

            var result = await repo.GetByAccountYearMonthAsync(42, 2021, 12);

            Assert.NotNull(result);
            Assert.Equal(42, result.AccountId);
            Assert.Equal(2021, result.Year);
            Assert.Equal(12, result.Month);
            Assert.Equal(123.45m, result.Balance);
            Assert.Equal(account, result.Account);
            Assert.Equal(audit, result.UploadAudit);
        }

        [Fact]
        public async Task GetByAccountYearMonthAsync_ReturnsNull_WhenNotExists()
        {
            await using var db = CreateContext();
            var repo = new MonthlyBalanceRepository(db);

            var result = await repo.GetByAccountYearMonthAsync(1, 2000, 1);

            Assert.Null(result);
        }

        [Fact]
        public async Task AddAsync_PersistsEntity_ToDatabase()
        {
            await using var db = CreateContext();

            var account = new Account { AccountId = 7, Name = "Inventory" };
            var audit = new UploadAudit { UploadAuditId = 55, FileName = "u.xlsx", UploadedAt = DateTime.UtcNow, ProcessStatus = UploadProcessStatus.Success };
            db.Accounts.Add(account);
            db.UploadAudits.Add(audit);
            await db.SaveChangesAsync();

            var repo = new MonthlyBalanceRepository(db);

            var toAdd = new MonthlyBalance
            {
                AccountId = 7,
                Account = account,
                Year = 2022,
                Month = 5,
                Balance = 999.99m,
                UploadAuditId = 55,
                UploadAudit = audit
            };

            await repo.AddAsync(toAdd);

            var all = await db.MonthlyBalances
                              .Include(mb => mb.Account)
                              .Include(mb => mb.UploadAudit)
                              .ToListAsync();

            Assert.Single(all);
            var fetched = all[0];
            Assert.Equal(7, fetched.AccountId);
            Assert.Equal("Inventory", fetched.Account.Name);
            Assert.Equal(2022, fetched.Year);
            Assert.Equal(5, fetched.Month);
            Assert.Equal(999.99m, fetched.Balance);
            Assert.Equal(55, fetched.UploadAuditId);
            Assert.Equal(audit, fetched.UploadAudit);
        }

        [Fact]
        public async Task GetByYearMonthAsync_ReturnsAllForYearMonth_AndIncludesNavigations()
        {
            await using var db = CreateContext();

            var acctA = new Account { AccountId = 1, Name = "A" };
            var acctB = new Account { AccountId = 2, Name = "B" };
            var audit1 = new UploadAudit { UploadAuditId = 10, FileName = "a1.xlsx", UploadedAt = DateTime.UtcNow, ProcessStatus = UploadProcessStatus.Success };
            var audit2 = new UploadAudit { UploadAuditId = 20, FileName = "a2.xlsx", UploadedAt = DateTime.UtcNow, ProcessStatus = UploadProcessStatus.Success };

            db.Accounts.AddRange(acctA, acctB);
            db.UploadAudits.AddRange(audit1, audit2);

            db.MonthlyBalances.AddRange(new[]
            {
                new MonthlyBalance { AccountId = 1, Account = acctA,  Year = 2023, Month = 6, Balance = 10m, UploadAuditId = 10, UploadAudit = audit1 },
                new MonthlyBalance { AccountId = 2, Account = acctB,  Year = 2023, Month = 6, Balance = 20m, UploadAuditId = 20, UploadAudit = audit2 },
                new MonthlyBalance { AccountId = 1, Account = acctA,  Year = 2023, Month = 5, Balance = 30m, UploadAuditId = 10, UploadAudit = audit1 }
            });
            await db.SaveChangesAsync();

            var repo = new MonthlyBalanceRepository(db);

            var juneBalances = await repo.GetByYearMonthAsync(2023, 6);

            Assert.Equal(2, juneBalances.Count);
            Assert.All(juneBalances, mb =>
            {
                Assert.NotNull(mb.Account);
                Assert.NotNull(mb.UploadAudit);
            });
            Assert.Contains(juneBalances, mb => mb.Account.Name == "A" && mb.Balance == 10m && mb.UploadAuditId == 10);
            Assert.Contains(juneBalances, mb => mb.Account.Name == "B" && mb.Balance == 20m && mb.UploadAuditId == 20);
        }

        [Fact]
        public async Task GetLatestYearMonthAsync_ReturnsCorrectLatestPair_WhenDataExists()
        {
            await using var db = CreateContext();

            var dummyAccount = new Account { AccountId = 1, Name = "X" };
            var dummyAudit = new UploadAudit { UploadAuditId = 5, FileName = "x.xlsx", UploadedAt = DateTime.UtcNow, ProcessStatus = UploadProcessStatus.Success };

            db.Accounts.Add(dummyAccount);
            db.UploadAudits.Add(dummyAudit);

            db.MonthlyBalances.AddRange(new[]
            {
                new MonthlyBalance { AccountId = 1, Account = dummyAccount, UploadAuditId = 5, UploadAudit = dummyAudit, Year = 2020, Month = 12, Balance = 1m },
                new MonthlyBalance { AccountId = 1, Account = dummyAccount, UploadAuditId = 5, UploadAudit = dummyAudit, Year = 2021, Month = 1,  Balance = 2m },
                new MonthlyBalance { AccountId = 1, Account = dummyAccount, UploadAuditId = 5, UploadAudit = dummyAudit, Year = 2021, Month = 5,  Balance = 3m },
                new MonthlyBalance { AccountId = 1, Account = dummyAccount, UploadAuditId = 5, UploadAudit = dummyAudit, Year = 2021, Month = 4,  Balance = 4m }
            });
            await db.SaveChangesAsync();

            var repo = new MonthlyBalanceRepository(db);

            var (year, month) = await repo.GetLatestYearMonthAsync();

            Assert.Equal(2021, year);
            Assert.Equal(5, month);
        }

        [Fact]
        public async Task GetLatestYearMonthAsync_ReturnsZeroZero_WhenNoData()
        {
            await using var db = CreateContext();
            var repo = new MonthlyBalanceRepository(db);

            var (year, month) = await repo.GetLatestYearMonthAsync();

            Assert.Equal(0, year);
            Assert.Equal(0, month);
        }
    }
}
