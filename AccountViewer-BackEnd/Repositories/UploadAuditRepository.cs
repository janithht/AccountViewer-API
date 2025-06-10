using AccountsViewer.Data;
using AccountsViewer.Entities;
using AccountsViewer.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace AccountsViewer.Repositories
{
    public class UploadAuditRepository : IUploadAuditRepository
    {
        private readonly AppDbContext _db;

        public UploadAuditRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<UploadAudit> CreateAsync(UploadAudit entity)
        {
            _db.UploadAudits.Add(entity);
            await _db.SaveChangesAsync();
            return entity;
        }

        public async Task UpdateStatusAsync(int uploadAuditId, UploadProcessStatus status, string? errorMessage = null)
        {
            var audit = await _db.UploadAudits.FindAsync(uploadAuditId);
            if (audit == null) return;

            audit.ProcessStatus = status;
            if (errorMessage != null)
                audit.ErrorMessage = errorMessage;

            _db.UploadAudits.Update(audit);
            await _db.SaveChangesAsync();
        }
    }
}
