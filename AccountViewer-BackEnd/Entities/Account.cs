namespace AccountsViewer.Entities
{
    public class Account
    {
        public int AccountId { get; set; }
        public required string Name { get; set; }
        public ICollection<MonthlyBalance> MonthlyBalances { get; set; } = new List<MonthlyBalance>();
    }
}
