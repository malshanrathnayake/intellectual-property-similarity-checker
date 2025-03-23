using System.Text.Json;
using System.Text.Json.Serialization;

namespace file_upload_api.Services
{
    public class BlockchainService
    {
        private readonly HttpClient _httpClient;

        public BlockchainService(HttpClient client)
        {
            _httpClient = client;
        }

        // Upload to IPFS
        public async Task<string> UploadToIPFS(Stream pdfStream, string fileName)
        {
            try
            {
                var form = new MultipartFormDataContent
                {
                    { new StreamContent(pdfStream), "file", fileName }
                };

                var response = await _httpClient.PostAsync("http://localhost:4000/ipfs/upload", form);
                response.EnsureSuccessStatusCode();

                var ipfsResult = await response.Content.ReadFromJsonAsync<IPFSResponse>();
                return ipfsResult?.Hash ?? throw new Exception("IPFS hash is null");
            }
            catch (Exception ex)
            {
                // Log or rethrow with context
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

                var response = await _httpClient.PostAsJsonAsync("http://localhost:4000/blockchain/register", payload);

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
