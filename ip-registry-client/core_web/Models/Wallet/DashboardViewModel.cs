using core_web.Services;

namespace core_web.Models.Wallet
{
    public class DashboardViewModel
    {
        public string Address { get; set; }
        public WalletNetWorth NetWorth { get; set; }
        public TokenBalanceResponse Tokens { get; set; }
        public TransactionResponse Transactions { get; set; }
        public NftResponse NFTs { get; set; }
        public WalletStatsResponse Stats { get; set; }
        public TokenTransferResponse TokenTransfers { get; set; }
        public NftTransferResponse NftTransfers { get; set; }
    }
}
