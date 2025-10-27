using System.Text.Json;
using System.Text.Json.Serialization;

namespace core_web.Services
{
    public class BlockchainService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpClientFactory _httpClientFactory;

        public BlockchainService(HttpClient client, IHttpClientFactory httpClientFactory)
        {
            _httpClient = client;
            _httpClientFactory = httpClientFactory;
        }

        // Upload to IPFS
        public async Task<string> UploadToIPFS(Stream pdfStream, string fileName, string title, string author)
        {
            try
            {
                pdfStream.Position = 0;

                var form = new MultipartFormDataContent
                {
                    { new StreamContent(pdfStream), "file", fileName },
                    { new StringContent(title), "title" },
                    { new StringContent(author), "author" }
                };

                var client = _httpClientFactory.CreateClient(); // or use injected _httpClient
                var response = await client.PostAsync("https://blockchain-api.azurewebsites.net/ipfs/upload", form);
                response.EnsureSuccessStatusCode();

                var ipfsResult = await response.Content.ReadFromJsonAsync<IPFSResponse>();
                return ipfsResult?.Hash ?? throw new Exception("IPFS hash is null");
            }
            catch (Exception ex)
            {
                throw new ApplicationException("IPFS upload failed: " + ex.Message, ex);
            }
        }




        // Register on Blockchain
        public async Task<string> RegisterPdfOnBlockchain(string ipfsHash, string walletAddress)
        {
            try
            {
                var payload = new
                {
                    ipfsHash,
                    walletAddress,
                    propertyType = "PDF"
                };

                var response = await _httpClient.PostAsJsonAsync("https://blockchain-api.azurewebsites.net/blockchain/register", payload);

                var responseBody = await response.Content.ReadAsStringAsync(); // Always read body

                if (!response.IsSuccessStatusCode)
                {
                    throw new ApplicationException($"Blockchain registration failed. Status: {response.StatusCode}, Response: {responseBody}");
                }

                var result = JsonSerializer.Deserialize<ContractResponse>(responseBody);

                if (result?.TokenId == null)
                {
                    throw new ApplicationException("Blockchain registration succeeded but TokenId is null.");
                }

                return result.TokenId;
            }
            catch (HttpRequestException httpEx)
            {
                throw new ApplicationException("HTTP request failed during blockchain registration: " + httpEx.Message, httpEx);
            }
            catch (JsonException jsonEx)
            {
                throw new ApplicationException("Failed to deserialize blockchain registration response: " + jsonEx.Message, jsonEx);
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Unexpected error during blockchain registration: " + ex.Message, ex);
            }
        }

        public async Task<string> GetOwnedNfts(string walletAddress)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://blockchain-api.azurewebsites.net/blockchain/tokens/{walletAddress}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException("GetOwnedNfts failed: " + content);

                return content;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error retrieving owned NFTs: " + ex.Message, ex);
            }
        }


        public async Task<string> TransferOwnership(string from, string to, string tokenId)
        {
            try
            {
                var payload = new
                {
                    from,
                    to,
                    tokenId
                };

                var response = await _httpClient.PostAsJsonAsync("https://blockchain-api.azurewebsites.net/blockchain/transfer", payload);

                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException("Transfer failed: " + content);

                return content;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Blockchain ownership transfer failed: " + ex.Message, ex);
            }
        }

        public async Task<string> GetOwnershipHistory(string tokenId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"https://blockchain-api.azurewebsites.net/blockchain/history/{tokenId}");
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    throw new ApplicationException("GetOwnershipHistory failed: " + content);

                return content;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Error retrieving ownership history: " + ex.Message, ex);
            }
        }


        public class IPFSResponse
        {
            public string Hash { get; set; }
        }

        public class ContractResponse
        {
            [JsonPropertyName("tokenId")]
            public string TokenId { get; set; }
        }
    }
}
