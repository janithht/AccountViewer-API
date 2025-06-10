namespace AccountsViewer.DTOs
{
    public class LatestBalancesDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public IList<BalanceDto> Items { get; set; }
    }
}
