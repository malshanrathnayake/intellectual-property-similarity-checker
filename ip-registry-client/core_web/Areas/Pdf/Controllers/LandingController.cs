using core_web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace core_web.Areas.Pdf.Controllers
{
    [Area("Pdf")]
    public class LandingController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly BlockchainService _blockchainService;

        public LandingController(IHttpClientFactory httpClientFactory, BlockchainService blockchainService)
        {
            _httpClientFactory = httpClientFactory;
            _blockchainService = blockchainService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string title, string author, IFormFile pdf)
        {
            if (pdf == null || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(author))
            {
                ModelState.AddModelError("", "All fields are required.");
                return View();
            }

            try
            {
                var client = _httpClientFactory.CreateClient();
                var similarityRequest = new MultipartFormDataContent();
                await using var similarityMs = new MemoryStream();
                await pdf.CopyToAsync(similarityMs);
                similarityMs.Position = 0;
                similarityRequest.Add(new StreamContent(similarityMs), "pdf", pdf.FileName);

                var response = await client.PostAsync("http://localhost:5000/check_similarity", similarityRequest);
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var similarityData = JsonSerializer.Deserialize<SimilarityResponse>(content);
                double similarity = similarityData?.similar_books?.FirstOrDefault()?.Similarity ?? 0;

                if (similarity >= 0.6)
                    throw new Exception($"PDF similarity too high ({similarity:P2}). Cannot register.");

                await using var pdfStream = new MemoryStream();
                await pdf.CopyToAsync(pdfStream);
                var ipfsHash = await _blockchainService.UploadToIPFS(pdfStream, pdf.FileName, title, author);

                var walletAddress = User.FindFirstValue("WalletAddress") ?? "0x87dc98819FAe36Ec910852D901f3eb4EBE8b024a";
                var tokenId = await _blockchainService.RegisterPdfOnBlockchain(ipfsHash, walletAddress);

                ViewBag.Success = true;
                ViewBag.TokenId = tokenId;
                ViewBag.IPFS = ipfsHash;
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }
        }

        public class SimilarityResponse
        {
            public List<SimilarPdf> similar_books { get; set; }
        }

        public class SimilarPdf
        {
            [JsonPropertyName("book_id")]
            public int BookId { get; set; }

            [JsonPropertyName("title")]
            public string Title { get; set; }

            [JsonPropertyName("author")]
            public string Author { get; set; }

            [JsonPropertyName("similarity")]
            public double Similarity { get; set; }
        }

    }
}
