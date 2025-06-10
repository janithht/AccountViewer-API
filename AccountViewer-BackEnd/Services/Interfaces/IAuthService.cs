using AccountsViewer.DTOs;

namespace AccountsViewer.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponseDto?> AuthenticateAsync(string username, string password);
    }
}
