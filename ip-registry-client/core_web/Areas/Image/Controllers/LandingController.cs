using core_web.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace core_web.Areas.Image.Controllers
{
    [Area("Image")]
    public class LandingController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ImageService _imageService;


        public LandingController(IHttpClientFactory httpClientFactory, ImageService imageService)
        {
            _httpClientFactory = httpClientFactory;
            _imageService = imageService;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("http://localhost:7000/get_all_metadata");
            var json = await response.Content.ReadAsStringAsync();
            
            ViewBag.MetadataJson = json;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Upload()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(ImageUploadDto model)
        {
            var walletAddress = User.FindFirstValue("WalletAddress")
                                ?? "0x87dc98819FAe36Ec910852D901f3eb4EBE8b024a";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //Read file into memory once
            byte[] fileBytes;
            await using (var ms = new MemoryStream())
            {
                await model.File.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            try
            {
                //Run Similarity
                var simResults = await _imageService.CheckSimilarityAsync(model, walletAddress, fileBytes);

                //Upload & Train
                var trainResponse = await _imageService.UploadAndTrainAsync(model, walletAddress, fileBytes);

                //Store into TempData for Results page
                TempData["UploadResult"] = JsonSerializer.Serialize(new
                {
                    Title = model.Title,
                    Category = model.Category,
                    Creator = model.CreatorName,
                    Description = model.Description,
                    PublishedSource = model.PublishedSource,
                    DateOfCreation = model.DateOfCreation?.ToString("yyyy-MM-dd"),
                    WalletAddress = walletAddress,
                    FileName = model.File.FileName,
                    TrainResult = trainResponse,
                    SimilarityResults = simResults
                });

                return RedirectToAction("Results");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View(model);
            }
        }


        [HttpGet]
        public IActionResult Results()
        {
            if (TempData["UploadResult"] is string resultJson)
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(resultJson);
                return View(data);
            }

            return RedirectToAction("Upload");
        }

        [HttpGet]
        public async Task<IActionResult> RegisterOnBlockchain(ImageMetadataDto model)
        {
            var walletAddress = User.FindFirstValue("WalletAddress")
                                ?? "0x87dc98819FAe36Ec910852D901f3eb4EBE8b024a";

            if (!ModelState.IsValid)
            {
                return View("Upload", model);
            }

            try
            {
                // Call service to upload metadata → IPFS → blockchain
                var (ipfsHash, tokenId) = await _imageService.RegisterImageAsync(model, walletAddress);

                // Save results for the view
                TempData["BlockchainResult"] = JsonSerializer.Serialize(new
                {
                    IpfsHash = ipfsHash,
                    BlockchainTokenId = tokenId,
                    WalletAddress = walletAddress
                });

                return RedirectToAction("Index", "Dashboard", new { area = "Blockchain" });

            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                return View("Upload", model);
            }
        }

        [HttpGet]
        public IActionResult BlockchainResult()
        {
            if (TempData["BlockchainResult"] is string resultJson)
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(resultJson);
                return View(data);
            }

            return RedirectToAction("Upload");
        }


    }

    public class ImageUploadDto
    {
        [Required]
        public string Title { get; set; }

        [Required]
        public string Category { get; set; }

        [Required]
        public string CreatorName { get; set; }

        public string Description { get; set; }

        public string PublishedSource { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfCreation { get; set; }

        [Required]
        public IFormFile File { get; set; }
    }

    public class ImageMetadataDto
    {
        [Required] public string Title { get; set; }
        [Required] public string Category { get; set; }
        [Required] public string CreatorName { get; set; }
        public string Description { get; set; }
        public string FileName { get; set; }
        public string PublishedSource { get; set; }
        public DateTime? DateOfCreation { get; set; }
    }

}
