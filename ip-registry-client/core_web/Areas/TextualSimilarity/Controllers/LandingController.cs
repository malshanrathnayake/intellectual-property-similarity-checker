using core_web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace core_web.Areas.Pdf.Controllers
{
    [Area("TextualSimilarity")]
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

        [HttpGet]
        public async Task<IActionResult> Similarity()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> RegisterResult()
        {
            return View();
        }

        // --------------------------
        // 1. Similarity Check Action
        // --------------------------
        [HttpPost]
        public async Task<IActionResult> Check(string title, string author, string language, IFormFile pdf)
        {
            if (pdf == null || string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(author) || string.IsNullOrWhiteSpace(language))
            {
                ModelState.AddModelError("", "All fields are required.");
                return View("Index");
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
                //response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    try
                    {
                        var json = JsonDocument.Parse(content);
                        if (json.RootElement.TryGetProperty("message", out var msg))
                        {
                            ViewBag.Error = msg.GetString();
                        }
                        else
                        {
                            ViewBag.Error = $"Flask API error ({response.StatusCode}): {content}";
                        }
                    }
                    catch
                    {
                        ViewBag.Error = $"Flask API error ({response.StatusCode}): {content}";
                    }

                    return View("Index");
                }

                var similarityData = JsonSerializer.Deserialize<SimilarityResponse>(content);
                var results = similarityData?.similar_books ?? new List<SimilarPdf>();
                double topScore = results.FirstOrDefault()?.Similarity ?? 0;

                // Pass values to Similarity.cshtml
                ViewBag.Results = results;
                ViewBag.TopScore = topScore;
                ViewBag.Title = title;
                ViewBag.Author = author;
                ViewBag.Language = language;

                // Save PDF temporarily on disk (use TempData to persist data)
                var tempFolder = Path.Combine(Path.GetTempPath(), "PatentUploads");
                Directory.CreateDirectory(tempFolder);
                var tempFilePath = Path.Combine(tempFolder, Guid.NewGuid() + "_" + pdf.FileName);

                await System.IO.File.WriteAllBytesAsync(tempFilePath, similarityMs.ToArray());

                // Save only metadata + path in TempData for use in Register action
                TempData["PdfPath"] = tempFilePath;
                TempData["Title"] = title;
                TempData["Author"] = author;
                TempData["Language"] = language;

                return View("Similarity");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Index");
            }
        }

        // --------------------------
        // 2. Register PDF Action
        // --------------------------
        [HttpPost]
        public async Task<IActionResult> Register()
        {
            try
            {
                if (TempData["PdfPath"] == null)
                {
                    throw new Exception("No PDF found to register. Please run similarity check first.");
                }

                // Retrieve metadata and PDF file from TempData
                string title = TempData["Title"]?.ToString() ?? "";
                string author = TempData["Author"]?.ToString() ?? "";
                string fileName = Path.GetFileName(TempData["PdfPath"]?.ToString() ?? "document.pdf");
                byte[] fileBytes = await System.IO.File.ReadAllBytesAsync(TempData["PdfPath"]?.ToString() ?? "");

                await using var pdfStream = new MemoryStream(fileBytes);
                var ipfsHash = await _blockchainService.UploadToIPFS(pdfStream, fileName, title, author);

                var walletAddress = User.FindFirstValue("WalletAddress") ?? "0x87dc98819FAe36Ec910852D901f3eb4EBE8b024a";
                var tokenId = await _blockchainService.RegisterPdfOnBlockchain(ipfsHash, walletAddress);

                ViewBag.Success = true;
                ViewBag.TokenId = tokenId;
                ViewBag.IPFS = ipfsHash;

                return View("RegisterResult");
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DownloadReport(string resultsJson, string filename)
        {
            try
            {
                // 1️⃣ Validate incoming data
                if (string.IsNullOrWhiteSpace(resultsJson))
                    return BadRequest("No results to include in the report.");

                // 2️⃣ Create HTTP client
                var client = _httpClientFactory.CreateClient();

                // 3️⃣ Build payload for Flask
                var payload = new
                {
                    filename = filename ?? "UploadedDocument.pdf",
                    similar_books = JsonSerializer.Deserialize<List<object>>(resultsJson)
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                // 4️⃣ Send to Flask
                var response = await client.PostAsync("http://localhost:5000/generate_report", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorText = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Flask API error ({response.StatusCode}): {errorText}");
                }

                // 5️⃣ Receive PDF from Flask and return it as a download
                var pdfBytes = await response.Content.ReadAsByteArrayAsync();

                return File(pdfBytes, "application/pdf", "Similarity_Report.pdf");
            }
            catch (Exception ex)
            {
                // Show a descriptive error
                ViewBag.Error = ex.Message;
                return View("Similarity");
            }
        }


        // --------------------------
        // Models for Similarity API
        // --------------------------
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

            // New metadata fields
            [JsonPropertyName("year")]
            public string Year { get; set; }

            [JsonPropertyName("language")]
            public string Language { get; set; }

            [JsonPropertyName("publisher")]
            public string Publisher { get; set; }

            [JsonPropertyName("rights")]
            public string Rights { get; set; }

            [JsonPropertyName("downloads")]
            public int Downloads { get; set; }

            [JsonPropertyName("word_count")]
            public int WordCount { get; set; }

            [JsonPropertyName("char_count")]
            public int CharCount { get; set; }

            [JsonPropertyName("text_path")]
            public string TextPath { get; set; }
        }

    }
}