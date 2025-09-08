using static core_web.Areas.Video.Controllers.LandingController;
using System.Text.Json.Serialization;

namespace core_web.Models
{
    public class VideoSimilarityResponse
    {
        [JsonPropertyName("similar_videos")]
        public List<SimilarVideo> SimilarVideos { get; set; }
    }

    public class SimilarVideo
    {
        [JsonPropertyName("filename")]
        public string Filename { get; set; }

        [JsonPropertyName("similarity")]
        public double Similarity { get; set; }
    }
}
