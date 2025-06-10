using AccountsViewer.Entities;

namespace AccountsViewer.Repositories.Interfaces
{
    public interface IUploadAuditRepository
    {
        Task<UploadAudit> CreateAsync(UploadAudit entity);
        Task UpdateStatusAsync(int uploadAuditId, UploadProcessStatus status, string? errorMessage = null);
    }
}
