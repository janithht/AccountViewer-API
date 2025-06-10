using AccountsViewer.Entities;
using AccountsViewer.Repositories.Interfaces;
using AccountsViewer.Services;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            _uploadService = new UploadService(
                _mockUnitOfWork.Object,
                _mockBlobClient.Object,
                _mockLogger.Object);
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
            mockFile.Setup(f => f.ContentType).Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            mockFile.Setup(f => f.OpenReadStream()).Returns(excelData);

            _mockUnitOfWork.Setup(u => u.Accounts.GetByNameAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Test exception"));

            var result = await _uploadService.ProcessFileAsync(mockFile.Object, "user1");

            Assert.False(result.Success);
            _mockUnitOfWork.Verify(u => u.RollbackAsync(), Times.Once);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("File upload failed")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public async Task ProcessFileAsync_BlobUploadFails_RollsBackTransaction()
        {
            // Arrange
            var excelData = CreateTestExcelFile();
            var mockFile = new Mock<IFormFile>();
            mockFile.Setup(f => f.FileName).Returns("test.xlsx");
            mockFile.Setup(f => f.ContentType).Returns("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            mockFile.Setup(f => f.OpenReadStream()).Returns(excelData);

            var mockAccount = new Account { AccountId = 1, Name = "Cash" };
            _mockUnitOfWork.Setup(u => u.Accounts.GetByNameAsync(It.IsAny<string>()))
                .ReturnsAsync(mockAccount);
            _mockUnitOfWork.Setup(u => u.MonthlyBalances.GetByAccountYearMonthAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(null as MonthlyBalance);

            var mockBlobClient = new Mock<BlobClient>();
            _mockBlobClient.Setup(c => c.GetBlobClient(It.IsAny<string>())).Returns(mockBlobClient.Object);
            mockBlobClient.Setup(c => c.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobUploadOptions>(), default))
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
