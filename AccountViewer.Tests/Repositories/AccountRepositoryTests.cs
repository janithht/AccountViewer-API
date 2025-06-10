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
    public class AccountRepositoryTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly AccountRepository _repository;

        public AccountRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            _repository = new AccountRepository(_dbContext);

            SeedTestData();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        private void SeedTestData()
        {
            var accounts = new List<Account>
            {
                new Account { AccountId = 1, Name = "Cash" },
                new Account { AccountId = 2, Name = "Bank Account" },
                new Account { AccountId = 3, Name = "Credit Card" },
                new Account { AccountId = 4, Name = "Alice's Account" },
                new Account { AccountId = 5, Name = "Bob’s Account" } 
            };

            _dbContext.Accounts.AddRange(accounts);
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task GetByNameAsync_ShouldReturnAccount_WhenNameExists()
        {
            var accountName = "Cash";

            var result = await _repository.GetByNameAsync(accountName);

            Assert.NotNull(result);
            Assert.Equal(accountName, result.Name);
        }

        [Fact]
        public async Task GetByNameAsync_ShouldTrimInput()
        {
            var accountName = "  Cash  ";

            var result = await _repository.GetByNameAsync(accountName);

            Assert.NotNull(result);
            Assert.Equal("Cash", result.Name);
        }

        [Fact]
        public async Task GetByNameAsync_ShouldThrowException_WhenAccountNotFound()
        {
            var accountName = "Non-existent Account";

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _repository.GetByNameAsync(accountName));
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllAccounts()
        {
            var result = await _repository.GetAllAsync();

            Assert.NotNull(result);
            Assert.Equal(5, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAccountsInCorrectOrder()
        {
            var result = await _repository.GetAllAsync();

            Assert.NotNull(result);
            Assert.Equal(1, result[0].AccountId);
            Assert.Equal(5, result[4].AccountId); 
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoAccountsExist()
        {
            _dbContext.Accounts.RemoveRange(_dbContext.Accounts);
            await _dbContext.SaveChangesAsync();

            var result = await _repository.GetAllAsync();

            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
