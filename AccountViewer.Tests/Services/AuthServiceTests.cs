using AccountsViewer.Data;
using Moq;
using AccountsViewer.DTOs;
using AccountsViewer.Entities;
using AccountsViewer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountViewer.Tests.Services
{
    public class AuthServiceTests
    {
        private readonly AuthService _authService;
        private readonly Mock<AppDbContext> _dbContextMock;
        private readonly Mock<IConfiguration> _configMock;

        public AuthServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var context = new AppDbContext(options);

            var passwordHash = BCrypt.Net.BCrypt.HashPassword("correct_password");
            context.Users.Add(new ApplicationUser
            {
                Id = 1,
                Username = "testuser",
                PasswordHash = passwordHash,
                Role = "User"
            });
            context.SaveChanges();

            _dbContextMock = new Mock<AppDbContext>(options);
            _configMock = new Mock<IConfiguration>();

            _configMock.Setup(c => c["Jwt:Key"]).Returns("ce50e454c5a2698028a2b72b6ade09566d7e22199e045c93a1666424563ffc3529d669dc26efa37d95e23167c55cb913b86254e79fad2e88e9490387f4e12a19f3cb41cb5396aaca9162e3f5469e79bfa5ac706e64c097c36cd7f6eb4247d5cb98aeb9f2055d28c6ff373ff10404f396b5415fc0418964fb071aafeb4e7431df962f285ccd7372909153dfcc5b168adb80295e449d0ecabba6fd8ce5a998c60a08e460c239e77a7d8b6599385869f61256b2edab3d857a835e99b83b7b853182b3c8e800f866948c4e9b4e9547ec8de24ab6c7284c18f604ae5cf4b532759a4b6827a3611ef8673f43d33ec50ba669f7399a8bb7ad7a7aa5cc61a76ba7d0058e");
            _configMock.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _configMock.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");
            _configMock.Setup(c => c["Jwt:DurationInMinutes"]).Returns("60");

            _authService = new AuthService(context, _configMock.Object);
        }


        [Fact]
        public async Task AuthenticateAsync_WithValidCredentials_ReturnsToken()
        {
            var result = await _authService.AuthenticateAsync("testuser", "correct_password");

            Assert.NotNull(result);
            Assert.False(string.IsNullOrEmpty(result.Token));
            Assert.True(result.Expires > System.DateTime.UtcNow);
        }

        [Fact]
        public async Task AuthenticateAsync_WithInvalidUsername_ReturnsNull()
        {
            var result = await _authService.AuthenticateAsync("wronguser", "correct_password");

            Assert.Null(result);
        }

        [Fact]
        public async Task AuthenticateAsync_WithInvalidPassword_ReturnsNull()
        {
            var result = await _authService.AuthenticateAsync("testuser", "wrong_password");

            Assert.Null(result);
        }
    }
}
