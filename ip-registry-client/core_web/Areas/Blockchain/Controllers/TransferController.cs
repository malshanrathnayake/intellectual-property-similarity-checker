using core_web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;

namespace core_web.Areas.Blockchain.Controllers
{
    [Area("Blockchain")]
    public class TransferController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public TransferController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index(string tokenId)
        {
            ViewBag.TokenId = tokenId;
            ViewBag.FromAddress = User.FindFirstValue("WalletAddress");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TransferOwnership(string tokenId, string toAddress)
        {
            var fromAddress = User.FindFirstValue("WalletAddress");

            try
            {
                var result = await TransferOwnership(fromAddress, toAddress, tokenId);
                TempData["SuccessMessage"] = $"Transfer successful! Tx Hash: {result.TxHash}";
                return RedirectToAction("Index", "Dashboard", new { area = "Blockchain" });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Transfer failed: " + ex.Message);
                ViewBag.TokenId = tokenId;
                ViewBag.FromAddress = fromAddress;
                return View();
            }
        }

        public async Task<TransferResponse> TransferOwnership(string from, string to, string tokenId)
        {
            var payload = new { from, to, tokenId };
            var client = _httpClientFactory.CreateClient();

            var response = await client.PostAsJsonAsync("http://localhost:4000/blockchain/transfer", payload);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new ApplicationException("Transfer failed: " + content);

            return JsonSerializer.Deserialize<TransferResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

    }
}
