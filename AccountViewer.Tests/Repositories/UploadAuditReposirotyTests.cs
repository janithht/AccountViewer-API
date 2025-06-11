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
    public class UploadAuditReposirotyTests
    {
        private static AppDbContext CreateContext()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(opts);
        }

        [Fact]
        public async Task CreateAsync_AddsEntity_AndReturnsItWithId()
        {
            await using var db = CreateContext();
            var repo = new UploadAuditRepository(db);

            var toCreate = new UploadAudit
            {
                UploadedAt = DateTime.UtcNow,
                FileName = "file.xlsx",
                StorageBlobUri = "https://blob",
                ProcessStatus = UploadProcessStatus.Pending
            };

            var result = await repo.CreateAsync(toCreate);

            Assert.Same(toCreate, result);
            Assert.True(result.UploadAuditId > 0);

            var fromDb = await db.UploadAudits.FirstOrDefaultAsync();
            Assert.NotNull(fromDb);
            Assert.Equal(result.UploadAuditId, fromDb.UploadAuditId);
            Assert.Equal("file.xlsx", fromDb.FileName);
            Assert.Equal("https://blob", fromDb.StorageBlobUri);
            Assert.Equal(UploadProcessStatus.Pending, fromDb.ProcessStatus);
            Assert.Null(fromDb.ErrorMessage);
        }

        [Fact]
        public async Task UpdateStatusAsync_UpdatesStatus_WhenEntityExists_AndKeepsErrorNull()
        {
            await using var db = CreateContext();
            var existing = new UploadAudit
            {
                UploadedAt = DateTime.UtcNow,
                FileName = "f.xlsx",
                StorageBlobUri = "u",
                ProcessStatus = UploadProcessStatus.Pending
            };
            db.UploadAudits.Add(existing);
            await db.SaveChangesAsync();

            var repo = new UploadAuditRepository(db);
            await repo.UpdateStatusAsync(existing.UploadAuditId, UploadProcessStatus.Success, null);

            var updated = await db.UploadAudits.FindAsync(existing.UploadAuditId);
            Assert.Equal(UploadProcessStatus.Success, updated.ProcessStatus);
            Assert.Null(updated.ErrorMessage);
        }

        [Fact]
        public async Task UpdateStatusAsync_UpdatesStatusAndErrorMessage_WhenErrorProvided()
        {
            await using var db = CreateContext();
            var existing = new UploadAudit
            {
                UploadedAt = DateTime.UtcNow,
                FileName = "f2.xlsx",
                StorageBlobUri = "u2",
                ProcessStatus = UploadProcessStatus.Pending
            };
            db.UploadAudits.Add(existing);
            await db.SaveChangesAsync();

            var repo = new UploadAuditRepository(db);
            await repo.UpdateStatusAsync(existing.UploadAuditId,
                                         UploadProcessStatus.Failed,
                                         "Something went wrong");

            var updated = await db.UploadAudits.FindAsync(existing.UploadAuditId);
            Assert.Equal(UploadProcessStatus.Failed, updated.ProcessStatus);
            Assert.Equal("Something went wrong", updated.ErrorMessage);
        }

        [Fact]
        public async Task UpdateStatusAsync_DoesNothing_WhenEntityDoesNotExist()
        {
            await using var db = CreateContext();
            var repo = new UploadAuditRepository(db);

            await repo.UpdateStatusAsync(999, UploadProcessStatus.Success, "ignored");

            Assert.Empty(db.UploadAudits);
        }
    }
}
