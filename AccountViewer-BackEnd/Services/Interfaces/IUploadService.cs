using AccountsViewer.DTOs;

namespace AccountsViewer.Services.Interfaces
{
    public interface IUploadService
    {
        Task<UploadResultDto> ProcessFileAsync(IFormFile file, string uploadedByUserId);
    }
}
