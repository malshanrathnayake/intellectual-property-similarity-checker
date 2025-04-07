using core_web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace core_web.Areas.Blockchain.Controllers
{
    [Area("Blockchain")]
    public class WalletController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public WalletController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var walletAddress = User.FindFirstValue("WalletAddress");
            if (string.IsNullOrEmpty(walletAddress))
                return RedirectToAction("Index", "Login", new { area = "Common" });

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"http://localhost:4000/blockchain/tokens/{walletAddress}");
            var content = await response.Content.ReadAsStringAsync();

            var properties = JsonSerializer.Deserialize<List<NFTViewModel>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ViewBag.Wallet = walletAddress;
            return View(properties);
        }

        [HttpGet]
        public async Task<IActionResult> TransactionHistory(string tokenId)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"http://localhost:4000/blockchain/history/{tokenId}");

            if (!response.IsSuccessStatusCode)
                return View(new List<TransactionHistoryViewModel>());

            var historyJson = await response.Content.ReadAsStringAsync();
            var history = JsonSerializer.Deserialize<List<TransactionHistoryViewModel>>(historyJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            ViewBag.TokenId = tokenId;
            return View(history);
        }
    }
}
