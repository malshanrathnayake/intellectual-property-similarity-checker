using core_web.Models.Wallet;
using core_web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace core_web.Areas.Blockchain.Controllers
{
    [Area("Blockchain")]
    public class DashboardController : Controller
    {
        private readonly WalletService _walletService;

        public DashboardController(WalletService walletService)
        {
            _walletService = walletService;
        }

        public async Task<IActionResult> Index()
        {
            var walletAddress = User.FindFirstValue("WalletAddress") ?? "0x87dc98819FAe36Ec910852D901f3eb4EBE8b024a";

            var netWorth = await _walletService.GetWalletNetWorth(walletAddress);
            var tokens = await _walletService.GetTokenBalances(walletAddress);
            var txs = await _walletService.GetTransactions(walletAddress);
            var nfts = await _walletService.GetNFTs(walletAddress);
            var stats = await _walletService.GetWalletStats(walletAddress);
            var tokenTransfers = await _walletService.GetTokenTransfers(walletAddress);
            var nftTransfers = await _walletService.GetNFTTransfers(walletAddress);


            var model = new DashboardViewModel
            {
                Address = walletAddress,
                NetWorth = netWorth,
                Tokens = tokens,
                Transactions = txs,
                NFTs = nfts,
                Stats = stats,
                TokenTransfers = tokenTransfers,
                NftTransfers = nftTransfers
            };

            return View(model);
        }
    }
}
