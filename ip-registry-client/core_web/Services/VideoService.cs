using core_web.Areas.Video.Controllers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace core_web.Services
{
    public class VideoService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public VideoService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // === Call Python similarity endpoint ===
        public async Task<VideoSimilarityResponse?> CheckSimilarityAsync(VideoUploadDto model, string walletAddress, byte[] fileBytes)
        {
            var client = _httpClientFactory.CreateClient();

            using var form = new MultipartFormDataContent();
            form.Add(new StreamContent(new MemoryStream(fileBytes)), "video", model.File.FileName);

            var response = await client.PostAsync("http://localhost:6000/check_video_similarity", form);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<VideoSimilarityResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // === Call Python train endpoint ===
        public async Task<TrainVideoResponse?> UploadAndTrainAsync(VideoUploadDto model, string walletAddress, byte[] fileBytes)
        {
            var client = _httpClientFactory.CreateClient();

            using var form = new MultipartFormDataContent();
            form.Add(new StreamContent(new MemoryStream(fileBytes)), "video", model.File.FileName);
            form.Add(new StringContent(model.Title), "title");
            form.Add(new StringContent(model.Category), "category");
            form.Add(new StringContent(model.Description ?? ""), "description");
            form.Add(new StringContent(model.PublishedSource ?? ""), "published_source");
            form.Add(new StringContent(model.CreatorName), "creator");

            var response = await client.PostAsync("http://localhost:6000/upload_and_train_video", form);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TrainVideoResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        // === Upload video metadata to IPFS ===
        public async Task<IPFSResponse> UploadMetadataToIPFS(VideoMetadataDto model, string walletAddress)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var payload = new
                {
                    filename = model.FileName,
                    title = model.Title,
                    category = model.Category,
                    creator = model.CreatorName,
                    description = model.Description,
                    published_source = model.PublishedSource,
                    date_of_creation = model.DateOfCreation?.ToString("yyyy-MM-dd"),
                    wallet_address = walletAddress
                };

                var response = await client.PostAsJsonAsync("http://localhost:4000/ipfs/registerVideo", payload);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"IPFS upload failed: {responseBody}");

                var result = JsonSerializer.Deserialize<IPFSResponse>(responseBody);

                if (result?.IpfsHash == null)
                    throw new ApplicationException("IPFS upload succeeded but IpfsHash is null.");

                return result;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error uploading video metadata to IPFS: " + ex.Message, ex);
            }
        }

        // === Register video metadata on blockchain ===
        public async Task<string> RegisterOnBlockchain(string ipfsHash, string walletAddress)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                var payload = new { ipfsHash, walletAddress };
                var response = await client.PostAsJsonAsync("http://localhost:4000/blockchain/register", payload);

                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException($"Blockchain registration failed: {responseBody}");

                var result = JsonSerializer.Deserialize<BlockchainResponse>(responseBody);

                if (result?.TokenId == null)
                    throw new ApplicationException("Blockchain registration succeeded but TokenId is null.");

                return result.TokenId;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error registering video on blockchain: " + ex.Message, ex);
            }
        }

        // === Full workflow: Upload metadata → IPFS, then register on Blockchain ===
        public async Task<(string IpfsHash, string TokenId)> RegisterVideoAsync(VideoMetadataDto model, string walletAddress)
        {
            // 1. Upload metadata to IPFS
            var ipfsResult = await UploadMetadataToIPFS(model, walletAddress);

            // 2. Register on blockchain
            var tokenId = await RegisterOnBlockchain(ipfsResult.IpfsHash, walletAddress);

            return (ipfsResult.IpfsHash, tokenId);
        }

        // === DTOs for Python responses ===
        public class SimilarVideo
        {
            public string Filename { get; set; }
            public double Similarity { get; set; }
        }

        public class VideoSimilarityResponse
        {
            public List<SimilarVideo> SimilarVideos { get; set; } = new();
        }

        public class TrainVideoResponse
        {
            public string Message { get; set; }
            public string DateOfCreation { get; set; }
        }

        // === DTOs for IPFS + Blockchain responses ===
        public class IPFSResponse
        {
            [JsonPropertyName("ipfsHash")]
            public string IpfsHash { get; set; }

            [JsonPropertyName("ipfsUrl")]
            public string IpfsUrl { get; set; }

            [JsonPropertyName("gatewayUrl")]
            public string GatewayUrl { get; set; }
        }

        public class BlockchainResponse
        {
            [JsonPropertyName("tokenId")]
            public string TokenId { get; set; }
        }
    }
}
