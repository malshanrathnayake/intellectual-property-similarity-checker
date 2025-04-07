using core_web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace core_web.Areas.Video.Controllers
{
    [Area("Video")]
    public class LandingController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public LandingController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string videoTitle, string creatorName, string category, IFormFile video)
        {
            if (video == null || string.IsNullOrWhiteSpace(videoTitle) || string.IsNullOrWhiteSpace(creatorName) || string.IsNullOrWhiteSpace(category))
            {
                ModelState.AddModelError("", "All fields are required.");
                return View();
            }

            try
            {
                var client = _httpClientFactory.CreateClient();

                //Check similarity
                var form = new MultipartFormDataContent();
                await using var videoStream = new MemoryStream();
                await video.CopyToAsync(videoStream);
                videoStream.Position = 0;
                form.Add(new StreamContent(videoStream), "video", video.FileName);

                var similarityResponse = await client.PostAsync("http://localhost:6000/check_video_similarity", form);
                similarityResponse.EnsureSuccessStatusCode();

                var responseBody = await similarityResponse.Content.ReadAsStringAsync();
                var similarityResult = JsonSerializer.Deserialize<VideoSimilarityResponse>(responseBody);
                var similarityScore = similarityResult?.SimilarVideos?.FirstOrDefault()?.Similarity ?? 0;

                //Train if similarity is low
                bool trained = false;
                if (similarityScore < 0.6)
                {
                    var trainForm = new MultipartFormDataContent();
                    videoStream.Position = 0; // Reset
                    trainForm.Add(new StreamContent(videoStream), "video", video.FileName);
                    var trainResponse = await client.PostAsync("http://localhost:6000/upload_and_train_video", trainForm);
                    trainResponse.EnsureSuccessStatusCode();
                    trained = true;
                }

                //Return results to view
                ViewBag.Success = true;
                ViewBag.Trained = trained;
                ViewBag.Similar = similarityResult?.SimilarVideos ?? new List<SimilarVideo>();
                ViewBag.SimilarityScore = similarityScore;
                return View();
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View();
            }
        }

    }
}
