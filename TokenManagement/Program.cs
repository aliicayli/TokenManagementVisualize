using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TokenManagement
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var tokenManager = new TokenManager();
            var apiService = new ApiService(tokenManager);

            var timer = new Timer(async (_) =>
            {
                await apiService.GetDataFromApiAsync();
                await apiService.GetDataFromApiAsync();

            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(5)); // 5 minutes timer

            Console.WriteLine("API Requests in every 5 minutes");
            Console.ReadKey();
        }
    }

    class TokenResponse
    {
        public string token_type { get; set; }
        public int expires_in { get; set; }
        public string access_token { get; set; }
    }

    class TokenManager
    {
        private TokenResponse _currentToken;
        private DateTime tokenExpireTime = DateTime.MinValue;

        private int hourlyRequestCount = 0;
        private DateTime hourlyCounterReset = DateTime.Now.AddHours(1);

        private readonly HttpClient _httpClient;

        public TokenManager()
        {
            _httpClient = new HttpClient();
        }

        public async Task<string> GetTokenAsync()
        {
            if (_currentToken != null && DateTime.Now < tokenExpireTime)
            {
                Console.WriteLine($"Token is already available. Available Time : {tokenExpireTime.ToShortTimeString()}");
                return _currentToken.access_token;
            }

            if (hourlyRequestCount >= 5)
            {
                Console.WriteLine($"hourly limit is only 5. Wait for 1 hour.");
                return null;
            }

            hourlyRequestCount++;
            Console.WriteLine($"New token operations ...");

            try
            {
                var tokenRequest = new
                {
                    grant_type = "client_credentials",
                    client_id = "api_client_id",
                    client_secret = "api_client_secret"
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(tokenRequest),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("https://api.exampleTokenApi", content);
                response.EnsureSuccessStatusCode();

                var responseContent = await response.Content.ReadAsStringAsync();
                _currentToken = JsonSerializer.Deserialize<TokenResponse>(responseContent);

                tokenExpireTime = DateTime.Now.AddSeconds(_currentToken.expires_in);

                Console.WriteLine($"new token is active: {tokenExpireTime.ToShortTimeString()}");
                return _currentToken.access_token;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error {ex.Message}");
                return null;
            }
        }

        public void ResetToken()
        {
            _currentToken = null;
            tokenExpireTime = DateTime.MinValue;
            Console.WriteLine("Reset token");
        }
    }

    class ApiService
    {
        private readonly TokenManager _tokenManager;
        private readonly HttpClient _httpClient;

        public ApiService(TokenManager tokenManager)
        {
            _tokenManager = tokenManager;
            _httpClient = new HttpClient();
        }

        public async Task GetDataFromApiAsync()
        {
            var token = await _tokenManager.GetTokenAsync();
            if (token == null)
            {
                Console.WriteLine("Error");
                return;
            }

            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token); // Adding auth for API request

                var response = await _httpClient.GetAsync("https://api.exampleTokenApi");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("This if statement means that token is available.");
                }
                else
                {
                    Console.WriteLine("Error");
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        Console.WriteLine("Token is unAuth");
                        _tokenManager.ResetToken();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error");
            }
        }
    }
}