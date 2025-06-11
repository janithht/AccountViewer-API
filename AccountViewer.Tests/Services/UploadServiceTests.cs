using System;
using System.IO;
using System.Threading.Tasks;
using AccountsViewer.Entities;
using AccountsViewer.Repositories.Interfaces;
using AccountsViewer.Services;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using OfficeOpenXml;
using Xunit;

namespace AccountViewer.Tests.Services
{
    public class UploadServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<BlobContainerClient> _mockBlobClient;
        private readonly Mock<ILogger<UploadService>> _mockLogger;
        private readonly UploadService _uploadService;

        public UploadServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockBlobClient = new Mock<BlobContainerClient>("UseDevelopmentStorage=true", "test-container");
            _mockLogger = new Mock<ILogger<UploadService>>();

            _mockUnitOfWork
                .Setup(u => u.BeginTransactionAsync())
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.CommitAsync())
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.RollbackAsync())
                .Returns(Task.CompletedTask);
            _mockUnitOfWork
                .Setup(u => u.SaveChangesAsync())
                .ReturnsAsync(1);

            _uploadService = new UploadService(
                _mockUnitOfWork.Object,
                _mockBlobClient.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task ProcessFileAsync_InvalidFileExtension_ReturnsError()
        {
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.pdf");
            mockFile.Setup(f => f.ContentType).Returns("application/pdf");

            var result = await _uploadService.ProcessFileAsync(mockFile.Object, "user1");

            Assert.False(result.Success);
            Assert.Equal("Only Excel files (.xlsx/.xls) are allowed", result.Message);
        }

        [Fact]
        public async Task ProcessFileAsync_ExceptionOccurs_RollsBackTransaction()
        {
            var excelData = CreateTestExcelFile();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.xlsx");
            mockFile.Setup(f => f.ContentType)
                    .Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            mockFile.Setup(f => f.OpenReadStream()).Returns(excelData);

            _mockUnitOfWork
                .Setup(u => u.Accounts.GetByNameAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test exception"));

            var result = await _uploadService.ProcessFileAsync(mockFile.Object, "user1");

            Assert.False(result.Success);
            _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
        }

        [Fact]
        public async Task ProcessFileAsync_ValidExcelFile_SuccessfullyProcessesBalances()
        {
            var excelData = CreateTestExcelFile();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.xlsx");
            mockFile.Setup(f => f.ContentType)
                    .Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            mockFile.Setup(f => f.OpenReadStream()).Returns(excelData);

            _mockUnitOfWork
               .Setup(u => u.MonthlyBalances.GetByAccountYearMonthAsync(
                   It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
               .ReturnsAsync(null as MonthlyBalance);

            _mockUnitOfWork
               .Setup(u => u.Accounts.GetByNameAsync(It.IsAny<string>()))
               .ReturnsAsync((string name) => new Account
               {
                   AccountId = name.GetHashCode(),
                   Name = name
               });

            var mockBlob = new Mock<BlobClient>();
            _mockBlobClient
                .Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(mockBlob.Object);

            mockBlob
                .Setup(c => c.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobUploadOptions>(),
                    default))
                .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());

            mockBlob
                .SetupGet(c => c.Uri)
                .Returns(new Uri("https://fake.blob.core.windows.net/container/blob.xlsx"));

            var mockUploadAuditRepo = new Mock<IUploadAuditRepository>();
            _mockUnitOfWork
                .Setup(u => u.UploadAudits)
                .Returns(mockUploadAuditRepo.Object);

            var createdAudit = new UploadAudit
            {
                UploadAuditId = 123,
                FileName = "test.xlsx"
            };
            mockUploadAuditRepo
                .Setup(r => r.CreateAsync(It.IsAny<UploadAudit>()))
                .ReturnsAsync(createdAudit);
            mockUploadAuditRepo
                .Setup(r => r.UpdateStatusAsync(
                    createdAudit.UploadAuditId,
                    UploadProcessStatus.Success,
                    It.Is<string?>(s => s == null)))
                .Returns(Task.CompletedTask);

            _mockUnitOfWork
               .Setup(u => u.MonthlyBalances.AddAsync(It.IsAny<MonthlyBalance>()))
               .Returns(Task.CompletedTask);

            var result = await _uploadService.ProcessFileAsync(mockFile.Object, "user1");

            Assert.True(result.Success);
            Assert.Equal(2023, result.Year);
            Assert.Equal(6, result.Month);
            Assert.Contains("Successfully processed balances for June 2023", result.Message);

            _mockBlobClient.Verify(c => c.GetBlobClient(It.IsAny<string>()), Times.Once);
            mockBlob.Verify(c => c.UploadAsync(
                It.IsAny<Stream>(),
                It.IsAny<BlobUploadOptions>(),
                default), Times.Once);

            _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
            _mockUnitOfWork.Verify(u => u.CommitAsync(), Times.Once);
            mockUploadAuditRepo.Verify(r => r.UpdateStatusAsync(
                createdAudit.UploadAuditId,
                UploadProcessStatus.Success,
                It.Is<string?>(s => s == null)), Times.Once);
            _mockUnitOfWork.Verify(u =>
                u.MonthlyBalances.AddAsync(It.IsAny<MonthlyBalance>()),
                Times.Exactly(5));
        }

        [Fact]
        public async Task ProcessFileAsync_BlobUploadFails_RollsBackTransaction()
        {
            var excelData = CreateTestExcelFile();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.xlsx");
            mockFile.Setup(f => f.ContentType)
                    .Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            mockFile.Setup(f => f.OpenReadStream()).Returns(excelData);

            var mockAccount = new Account { AccountId = 1, Name = "Cash" };
            _mockUnitOfWork
                .Setup(u => u.Accounts.GetByNameAsync(It.IsAny<string>()))
                .ReturnsAsync(mockAccount);
            _mockUnitOfWork
                .Setup(u => u.MonthlyBalances.GetByAccountYearMonthAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(null as MonthlyBalance);

            var mockBlob = new Mock<BlobClient>();
            _mockBlobClient
                .Setup(c => c.GetBlobClient(It.IsAny<string>()))
                .Returns(mockBlob.Object);
            mockBlob
                .Setup(c => c.UploadAsync(
                    It.IsAny<Stream>(),
                    It.IsAny<BlobUploadOptions>(),
                    default))
                .ThrowsAsync(new Exception("Blob upload failed"));

            var result = await _uploadService.ProcessFileAsync(mockFile.Object, "user1");

            Assert.False(result.Success);
            _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
            Assert.Contains("Blob upload failed", result.Message);
        }

        private MemoryStream CreateTestExcelFile()
        {
            ExcelPackage.License.SetNonCommercialPersonal("Test Organization");

            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Sheet1");

            worksheet.Cells[1, 1].Value = "Account Balances for June 2023";

            worksheet.Cells[2, 1].Value = "Cash";
            worksheet.Cells[2, 2].Value = "1000.50";

            worksheet.Cells[3, 1].Value = "Accounts Receivable";
            worksheet.Cells[3, 2].Value = "2000.75";

            worksheet.Cells[4, 1].Value = "Inventory";
            worksheet.Cells[4, 2].Value = "3000.25";

            worksheet.Cells[5, 1].Value = "Accounts Payable";
            worksheet.Cells[5, 2].Value = "-1500.50";

            worksheet.Cells[6, 1].Value = "Loans Payable";
            worksheet.Cells[6, 2].Value = "-5000.00";

            var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            return stream;
        }
    }
}
