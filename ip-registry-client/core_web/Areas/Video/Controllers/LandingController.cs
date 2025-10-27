using core_web.Models;
using core_web.Services;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace core_web.Areas.Video.Controllers
{
    [Area("Video")]
    public class LandingController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly VideoService _videoService;

        public LandingController(IHttpClientFactory httpClientFactory, VideoService videoService)
        {
            _httpClientFactory = httpClientFactory;
            _videoService = videoService;
        }

        // --- Index page: load all metadata ---
        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://video-similarity-checker.azurewebsites.net/get_all_video_metadata");
            var json = await response.Content.ReadAsStringAsync();

            ViewBag.MetadataJson = json;
            return View();
        }

        // --- Upload form ---
        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(VideoUploadDto model)
        {
            var walletAddress = User.FindFirstValue("WalletAddress")
                                ?? "0x87dc98819FAe36Ec910852D901f3eb4EBE8b024a";

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Read video file into memory once
            byte[] fileBytes;
            await using (var ms = new MemoryStream())
            {
                await model.File.CopyToAsync(ms);
                fileBytes = ms.ToArray();
            }

            try
            {
                // Run similarity
                var simResults = await _videoService.CheckSimilarityAsync(model, walletAddress, fileBytes);

                // Upload & Train
                var trainResponse = await _videoService.UploadAndTrainAsync(model, walletAddress, fileBytes);

                // Store into TempData for Results page
                TempData["VideoUploadResult"] = JsonSerializer.Serialize(new
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

        // --- Results page ---
        [HttpGet]
        public IActionResult Results()
        {
            if (TempData["VideoUploadResult"] is string resultJson)
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, object>>(resultJson);
                return View(data);
            }

            return RedirectToAction("Upload");
        }

        // --- Register on Blockchain ---
        [HttpGet]
        public async Task<IActionResult> RegisterOnBlockchain(VideoMetadataDto model)
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
                var (ipfsHash, tokenId) = await _videoService.RegisterVideoAsync(model, walletAddress);

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

    // --- DTOs ---
    public class VideoUploadDto
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

    public class VideoMetadataDto
    {
        [Required] public string Title { get; set; }
        [Required] public string Category { get; set; }
        [Required] public string CreatorName { get; set; }
        public string Description { get; set; }
        public string FileName { get; set; }
        public string PublishedSource { get; set; }
        public DateTime? DateOfCreation { get; set; }
    }

    ////[Area("Video")]
    ////public class LandingController : Controller
    ////{
    ////    private readonly IHttpClientFactory _httpClientFactory;

    ////    public LandingController(IHttpClientFactory httpClientFactory)
    ////    {
    ////        _httpClientFactory = httpClientFactory;
    ////    }

    ////    [HttpGet]
    ////    public IActionResult Index()
    ////    {
    ////        return View();
    ////    }

    ////    [HttpPost]
    ////    public async Task<IActionResult> Index(string videoTitle, string creatorName, string category, IFormFile video)
    ////    {
    ////        if (video == null || string.IsNullOrWhiteSpace(videoTitle) || string.IsNullOrWhiteSpace(creatorName) || string.IsNullOrWhiteSpace(category))
    ////        {
    ////            ModelState.AddModelError("", "All fields are required.");
    ////            return View();
    ////        }

    ////        try
    ////        {
    ////            var client = _httpClientFactory.CreateClient();

    ////            //Check similarity
    ////            var form = new MultipartFormDataContent();
    ////            await using var videoStream = new MemoryStream();
    ////            await video.CopyToAsync(videoStream);
    ////            videoStream.Position = 0;
    ////            form.Add(new StreamContent(videoStream), "video", video.FileName);

    ////            var similarityResponse = await client.PostAsync("http://localhost:6000/check_video_similarity", form);
    ////            similarityResponse.EnsureSuccessStatusCode();

    ////            var responseBody = await similarityResponse.Content.ReadAsStringAsync();
    ////            var similarityResult = JsonSerializer.Deserialize<VideoSimilarityResponse>(responseBody);
    ////            var similarityScore = similarityResult?.SimilarVideos?.FirstOrDefault()?.Similarity ?? 0;

    ////            //Train if similarity is low
    ////            bool trained = false;
    ////            if (similarityScore < 0.6)
    ////            {
    ////                var trainForm = new MultipartFormDataContent();
    ////                videoStream.Position = 0; // Reset
    ////                trainForm.Add(new StreamContent(videoStream), "video", video.FileName);
    ////                var trainResponse = await client.PostAsync("http://localhost:6000/upload_and_train_video", trainForm);
    ////                trainResponse.EnsureSuccessStatusCode();
    ////                trained = true;
    ////            }

    ////            //Return results to view
    ////            ViewBag.Success = true;
    ////            ViewBag.Trained = trained;
    ////            ViewBag.Similar = similarityResult?.SimilarVideos ?? new List<SimilarVideo>();
    ////            ViewBag.SimilarityScore = similarityScore;
    ////            return View();
    ////        }
    ////        catch (Exception ex)
    ////        {
    ////            ViewBag.Error = ex.Message;
    ////            return View();
    ////        }
    ////    }

    ////}
}
