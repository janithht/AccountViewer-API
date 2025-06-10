using AccountsViewer.DTOs;
using AccountsViewer.Entities;
using AccountsViewer.Repositories.Interfaces;
using AccountsViewer.Services.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AccountsViewer.Services
{
    public class UploadService : IUploadService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly BlobContainerClient _blobClient;
        private readonly ILogger<UploadService> _logger;

        public UploadService(
            IUnitOfWork unitOfWork,
            BlobContainerClient blobClient,
            ILogger<UploadService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;

            
            _blobClient = blobClient;
        }

        public async Task<UploadResultDto> ProcessFileAsync(IFormFile file, string userId)
        {
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
            {
                return new UploadResultDto
                {
                    Success = false,
                    Message = "Only Excel files (.xlsx/.xls) are allowed"
                };
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var parsed = await ParseExcelAsync(file);

                foreach (var (name, balance) in parsed.AccountBalances)
                {
                    var account = await _unitOfWork.Accounts.GetByNameAsync(name)
                                  ?? throw new ArgumentException($"Unknown account: {name}");

                    var exists = await _unitOfWork.MonthlyBalances.GetByAccountYearMonthAsync(
                        account.AccountId, parsed.Year, parsed.Month
                    );

                    if (exists != null)
                    {
                        throw new InvalidOperationException(
                            $"Balances for {name} in {parsed.Month}/{parsed.Year} exist."
                        );
                    }
                }

                var blobName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid()}{ext}";
                var blobClient = _blobClient.GetBlobClient(blobName);
                await using (var stream = file.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, new BlobUploadOptions
                    {
                        HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
                    });
                }

                var audit = new UploadAudit
                {
                    UploadedAt = DateTime.UtcNow,
                    FileName = file.FileName,
                    StorageBlobUri = blobClient.Uri.ToString(),
                    ProcessStatus = UploadProcessStatus.Pending
                };
                var createdAudit = await _unitOfWork.UploadAudits.CreateAsync(audit);
                await _unitOfWork.SaveChangesAsync();

                foreach (var (name, balance) in parsed.AccountBalances)
                {
                    var account = await _unitOfWork.Accounts.GetByNameAsync(name);

                    await _unitOfWork.MonthlyBalances.AddAsync(new MonthlyBalance
                    {
                        AccountId = account.AccountId,
                        Account = account,
                        Year = parsed.Year,
                        Month = parsed.Month,
                        Balance = balance,
                        UploadAuditId = createdAudit.UploadAuditId,
                        UploadAudit = createdAudit
                    });
                }

                await _unitOfWork.UploadAudits.UpdateStatusAsync(
                    createdAudit.UploadAuditId,
                    UploadProcessStatus.Success
                );

                await _unitOfWork.CommitAsync();
                await _unitOfWork.SaveChangesAsync();

                return new UploadResultDto
                {
                    Success = true,
                    Year = parsed.Year,
                    Month = parsed.Month,
                    Message = $"Successfully processed balances for " +
                              $"{CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(parsed.Month)} " +
                              $"{parsed.Year}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File upload failed");
                await _unitOfWork.RollbackAsync();

                return new UploadResultDto
                {
                    Success = false,
                    Message = ex.Message
                };
            }
            finally
            {
                _unitOfWork.Dispose();
            }
        }

        private async Task<ParsedBalances> ParseExcelAsync(IFormFile file)
        {
            ExcelPackage.License.SetNonCommercialPersonal("Test Organization");

            using var package = new ExcelPackage();
            await package.LoadAsync(file.OpenReadStream());

            var worksheet = package.Workbook.Worksheets.FirstOrDefault()
                ?? throw new ArgumentException("Excel file contains no worksheets");

            var header = worksheet.Cells[1, 1].Text?.Trim()
                ?? throw new ArgumentException("Missing header in cell A1");

            var match = Regex.Match(header, @"for\s+([A-Za-z]+)\s+(\d{4})", RegexOptions.IgnoreCase);
            if (!match.Success || match.Groups.Count != 3)
            {
                throw new ArgumentException("Invalid header format. Expected format: 'Account Balances for Month Year'");
            }

            var monthName = match.Groups[1].Value;
            var yearString = match.Groups[2].Value;

            if (!int.TryParse(yearString, out int year))
            {
                throw new ArgumentException($"Invalid year value: {yearString}");
            }

            if (!DateTime.TryParseExact(
                monthName,
                "MMMM",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
            {
                throw new ArgumentException($"Invalid month name: {monthName}");
            }
            var month = date.Month;

            var balances = new List<(string, decimal)>();

            for (int row = 2; row <= 6; row++)
            {
                var accountName = worksheet.Cells[row, 1].Text?.Trim()
                    ?? throw new ArgumentException($"Missing account name in row {row}");

                accountName = accountName.Replace("’", "'").Trim();

                var balanceValue = worksheet.Cells[row, 2].Text?.Trim()
                    ?? throw new ArgumentException($"Missing balance value in row {row}");

                if (!decimal.TryParse(
                    balanceValue.Replace(",", ""),
                    NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                    CultureInfo.InvariantCulture,
                    out decimal balance))
                {
                    throw new ArgumentException($"Invalid balance format in row {row}: {balanceValue}");
                }

                balances.Add((accountName, balance));
            }

            if (balances.Count != 5)
            {
                throw new ArgumentException($"Expected 5 account balances, found {balances.Count}");
            }

            return new ParsedBalances
            {
                Year = year,
                Month = month,
                AccountBalances = balances
            };
        }

        private class ParsedBalances
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public List<(string AccountName, decimal Balance)> AccountBalances { get; set; } = new();
        }
    }
}
