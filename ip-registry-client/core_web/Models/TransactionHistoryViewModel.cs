namespace core_web.Models
{
    public class TransactionHistoryViewModel
    {
        public string From { get; set; }
        public string To { get; set; }
        public string TokenId { get; set; }
        public string Timestamp { get; set; }
        public string TxHash { get; set; }
    }
}
