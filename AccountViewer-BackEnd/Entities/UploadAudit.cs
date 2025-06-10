namespace AccountsViewer.Entities
{
    public enum UploadProcessStatus
    {
        Pending = 0,
        Success = 1,
        Failed = 2
    }
    public class UploadAudit
    {
        public int UploadAuditId { get; set; }   
        public DateTime UploadedAt { get; set; }     

        public string? FileName { get; set; }
        public string? StorageBlobUri { get; set; }

        public UploadProcessStatus ProcessStatus { get; set; }
        public string? ErrorMessage { get; set; }

        public ICollection<MonthlyBalance>? MonthlyBalances { get; set; }
    }
}
