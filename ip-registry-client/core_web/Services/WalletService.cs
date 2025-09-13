using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace core_web.Services
{
    public class WalletService
    {
        private readonly HttpClient _httpClient;
        private readonly string _moralisApiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJub25jZSI6ImM5MzBkMjkxLTI5MmItNGNiNy04M2NiLTZjMGNjNDM0ZGExOSIsIm9yZ0lkIjoiNDI2ODUwIiwidXNlcklkIjoiNDM5MDUxIiwidHlwZUlkIjoiNGMxY2U2ZTctMzdlMy00NWI1LTk3YzQtOWM5N2Y0MWIwZjU5IiwidHlwZSI6IlBST0pFQ1QiLCJpYXQiOjE3Mzc0Njk2NzgsImV4cCI6NDg5MzIyOTY3OH0.XPAe6UmYHBR4ArNzW1zDS1DT8RSo3sbhrklbRlQjtBk";

        public WalletService(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _moralisApiKey);
        }

        private async Task<T?> GetAsync<T>(string url)
        {
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Moralis API Error: {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        //Get Net Worth
        public Task<WalletNetWorth?> GetWalletNetWorth(string address, string chain = "sepolia")
        {
            var url = $"https://deep-index.moralis.io/api/v2.2/wallets/{address}/net-worth?chain={chain}";
            return GetAsync<WalletNetWorth>(url);
        }

        //Get Token Balances
        public Task<TokenBalanceResponse?> GetTokenBalances(string address, string chain = "sepolia")
        {
            var url = $"https://deep-index.moralis.io/api/v2.2/wallets/{address}/tokens?chain={chain}";
            return GetAsync<TokenBalanceResponse>(url);
        }

        //Get Token Transfers
        public Task<TokenTransferResponse?> GetTokenTransfers(string address, string chain = "sepolia", string order = "DESC")
        {
            var url = $"https://deep-index.moralis.io/api/v2.2/{address}/erc20/transfers?chain={chain}&order={order}";
            return GetAsync<TokenTransferResponse>(url);
        }


        //Get Transactions
        public Task<TransactionResponse?> GetTransactions(string address, string chain = "sepolia", string order = "DESC")
        {
            var url = $"https://deep-index.moralis.io/api/v2.2/{address}?chain={chain}&order={order}";
            return GetAsync<TransactionResponse>(url);
        }


        //Get NFTs owned by wallet
        public Task<NftResponse?> GetNFTs(string address, string chain = "sepolia")
        {
            var url =
                $"https://deep-index.moralis.io/api/v2.2/{address}/nft?chain={chain}&format=decimal&normalizeMetadata=true&media_items=false&include_prices=false";
            return GetAsync<NftResponse>(url);
        }

        // Get NFT transfers by wallet
        public Task<NftTransferResponse?> GetNFTTransfers(string address, string chain = "sepolia", string order = "DESC")
        {
            var url =
                $"https://deep-index.moralis.io/api/v2.2/{address}/nft/transfers?chain={chain}&format=decimal&order={order}";
            return GetAsync<NftTransferResponse>(url);
        }

        //Get Wallet Stats
        public Task<WalletStatsResponse?> GetWalletStats(string address, string chain = "sepolia")
        {
            var url = $"https://deep-index.moralis.io/api/v2.2/wallets/{address}/stats?chain={chain}";
            return GetAsync<WalletStatsResponse>(url);
        }
    }

    public class WalletNetWorth
    {
        public string Total_Networth_Usd { get; set; }
        public List<ChainNetWorth> Chains { get; set; }
    }

    public class ChainNetWorth
    {
        public string Chain { get; set; }
        public string Native_Balance { get; set; }
        public string Native_Balance_Formatted { get; set; }
        public string Native_Balance_Usd { get; set; }
        public string Token_Balance_Usd { get; set; }
        public string Networth_Usd { get; set; }
    }


    public class TokenBalanceResponse
    {
        public string Cursor { get; set; }
        public int Page { get; set; }
        public int Page_Size { get; set; }
        public long Block_Number { get; set; }
        public List<TokenBalance> Result { get; set; }
    }

    public class TokenBalance
    {
        public string Token_Address { get; set; }
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string Logo { get; set; }
        public string Thumbnail { get; set; }
        public int Decimals { get; set; }
        public string Balance { get; set; }
        public string Balance_Formatted { get; set; }
        public decimal Usd_Price { get; set; }
        public decimal Usd_Value { get; set; }
        public bool Native_Token { get; set; }
    }


    public class TokenTransferResponse
    {
        public int Page { get; set; }
        public int Page_Size { get; set; }
        public string Cursor { get; set; }
        public List<TokenTransfer> Result { get; set; }
    }

    public class TokenTransfer
    {
        public string Token_Name { get; set; }
        public string Token_Symbol { get; set; }
        public string Token_Logo { get; set; }
        public string Token_Decimals { get; set; }

        public string From_Address { get; set; }
        public string To_Address { get; set; }
        public string Address { get; set; }

        public string Block_Hash { get; set; }
        public string Block_Number { get; set; }
        public DateTime Block_Timestamp { get; set; }

        public string Transaction_Hash { get; set; }
        public int Transaction_Index { get; set; }
        public int Log_Index { get; set; }

        public string Value { get; set; }          // raw value in wei
        public string Value_Decimal { get; set; }  // already human-friendly
        public bool Possible_Spam { get; set; }
        public bool Verified_Contract { get; set; }
    }


    public class TransactionResponse
    {
        public string Cursor { get; set; }
        public int Page { get; set; }
        public int Page_Size { get; set; }
        public List<Transaction> Result { get; set; }
    }

    public class Transaction
    {
        public string Hash { get; set; }
        public string Nonce { get; set; }
        public string Transaction_Index { get; set; }
        public string From_Address { get; set; }
        public string To_Address { get; set; }
        public string Value { get; set; }
        public string Gas { get; set; }
        public string Gas_Price { get; set; }
        public string Input { get; set; }
        public string Receipt_Status { get; set; }
        public DateTime Block_Timestamp { get; set; }
        public string Block_Number { get; set; }
        public string Block_Hash { get; set; }
        public string Transaction_Fee { get; set; }
    }


    public class NftResponse
    {
        public string Status { get; set; }
        public int Page { get; set; }
        public int Page_Size { get; set; }
        public string Cursor { get; set; }
        public List<NftOwned> Result { get; set; }
    }

    public class NftOwned
    {
        public string Amount { get; set; }
        public string Token_Id { get; set; }
        public string Token_Address { get; set; }
        public string Contract_Type { get; set; }
        public string Owner_Of { get; set; }
        public string Block_Number { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }

        public NftListPrice List_Price { get; set; }
    }

    public class NftListPrice
    {
        public bool Listed { get; set; }
        public string Price { get; set; }
        public string Price_Currency { get; set; }
        public string Price_Usd { get; set; }
        public string Marketplace { get; set; }
    }


    public class NftTransferResponse
    {
        public int Page { get; set; }
        public int Page_Size { get; set; }
        public string Cursor { get; set; }
        public bool Block_Exists { get; set; }
        public List<NftTransfer> Result { get; set; }
    }

    public class NftTransfer
    {
        public string Block_Number { get; set; }
        public DateTime Block_Timestamp { get; set; }
        public string Block_Hash { get; set; }

        public string Transaction_Hash { get; set; }
        public int Transaction_Index { get; set; }
        public int Log_Index { get; set; }

        public string Value { get; set; }
        public string Contract_Type { get; set; }
        public string Transaction_Type { get; set; }

        public string Token_Address { get; set; }
        public string Token_Id { get; set; }

        public string From_Address { get; set; }
        public string From_Address_Entity { get; set; }
        public string From_Address_Label { get; set; }

        public string To_Address { get; set; }
        public string To_Address_Entity { get; set; }
        public string To_Address_Label { get; set; }

        public string Amount { get; set; }
        public int Verified { get; set; }
        public bool Possible_Spam { get; set; }
    }

    public class WalletStatsResponse
    {
        public string Nfts { get; set; }
        public string Collections { get; set; }
        public TransactionStats Transactions { get; set; }
        public TransferStats Nft_Transfers { get; set; }
        public TransferStats Token_Transfers { get; set; }
    }

    public class TransactionStats
    {
        public string Total { get; set; }
    }

    public class TransferStats
    {
        public string Total { get; set; }
    }

}
