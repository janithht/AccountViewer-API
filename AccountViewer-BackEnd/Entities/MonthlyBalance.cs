namespace AccountsViewer.Entities
{
    public class MonthlyBalance
    {
        public int MonthlyBalanceId { get; set; }
        public int AccountId { get; set; }
        public required Account Account { get; set; }

        public int Year { get; set; }
        public int Month { get; set; }
        public decimal Balance { get; set; }

        public int UploadAuditId { get; set; }
        public required UploadAudit UploadAudit { get; set; }
    }
}
