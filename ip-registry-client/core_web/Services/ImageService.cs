using core_web.Areas.Image.Controllers;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace core_web.Services
{
    public class ImageService
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ImageService(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        /// <summary>
        /// Uploads image + metadata to train endpoint
        /// </summary>
        public async Task<string> UploadAndTrainAsync(ImageUploadDto model, string walletAddress, byte[] fileBytes)
        {
            var client = _httpClientFactory.CreateClient();

            using var form = BuildForm(model, walletAddress, fileBytes);
            var response = await client.PostAsync("http://localhost:7000/upload_and_train_image", form);

            var apiResponse = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception("Training failed: " + apiResponse);

            return apiResponse;
        }

        /// <summary>
        /// Runs similarity check for the uploaded image
        /// </summary>
        public async Task<List<Dictionary<string, object>>> CheckSimilarityAsync(ImageUploadDto model, string walletAddress, byte[] fileBytes)
        {
            var client = _httpClientFactory.CreateClient();

            using var form = BuildForm(model, walletAddress, fileBytes);
            var response = await client.PostAsync("http://localhost:7000/check_image_similarity", form);

            var apiResponse = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
                throw new Exception("Similarity check failed: " + apiResponse);

            using var doc = JsonDocument.Parse(apiResponse);
            var results = new List<Dictionary<string, object>>();

            if (doc.RootElement.TryGetProperty("similar_images", out var simArray) && simArray.ValueKind == JsonValueKind.Array)
            {
                results = simArray.EnumerateArray()
                                  .Select(el => JsonSerializer.Deserialize<Dictionary<string, object>>(el.GetRawText()))
                                  .ToList()!;
            }

            return results;
        }

        /// <summary>
        /// Builds multipart form with metadata + image stream
        /// </summary>
        private MultipartFormDataContent BuildForm(ImageUploadDto model, string walletAddress, byte[] fileBytes)
        {
            var form = new MultipartFormDataContent();

            form.Add(new StringContent(model.Title), "title");
            form.Add(new StringContent(model.Category), "category");
            form.Add(new StringContent(model.CreatorName), "creator");
            if (!string.IsNullOrEmpty(model.Description))
                form.Add(new StringContent(model.Description), "description");
            if (!string.IsNullOrEmpty(model.PublishedSource))
                form.Add(new StringContent(model.PublishedSource), "published_source");
            if (model.DateOfCreation.HasValue)
                form.Add(new StringContent(model.DateOfCreation.Value.ToString("yyyy-MM-dd")), "date_of_creation");

            form.Add(new StringContent(walletAddress), "wallet_address");

            var fileStream = new MemoryStream(fileBytes);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(model.File.ContentType);
            form.Add(fileContent, "image", model.File.FileName);

            return form;
        }

        /// <summary>
        /// Upload image metadata to IPFS (via Node.js backend).
        /// </summary>
        public async Task<IPFSResponse> UploadMetadataToIPFS(ImageMetadataDto model, string walletAddress)
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

                var response = await client.PostAsJsonAsync("http://localhost:4000/ipfs/registerImage", payload);
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
                throw new ApplicationException("Error uploading metadata to IPFS: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Register the uploaded image metadata on blockchain.
        /// </summary>
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
                throw new ApplicationException("Error registering on blockchain: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// Full workflow: Upload metadata → IPFS, then register on Blockchain.
        /// </summary>
        public async Task<(string IpfsHash, string TokenId)> RegisterImageAsync(ImageMetadataDto model, string walletAddress)
        {
            // 1. Upload metadata to IPFS
            var ipfsResult = await UploadMetadataToIPFS(model, walletAddress);

            // 2. Register on blockchain
            var tokenId = await RegisterOnBlockchain(ipfsResult.IpfsHash, walletAddress);

            return (ipfsResult.IpfsHash, tokenId);
        }

        // DTOs
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
