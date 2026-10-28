using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using static Models.Dtos.AuthDtos;

namespace planno_MAUI.Services
{
    public class AuthService(HttpClient httpClient)
    {
        private const string TokenKey = "zugangsToken";
        private readonly HttpClient _httpClient = httpClient;
        private string? _token;

        public async Task RegisterAsync(RegisterRequest registerRequest)
        {
            var response = await _httpClient.PostAsJsonAsync("Auth/register", registerRequest);
            response.EnsureSuccessStatusCode();
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrEmpty(password))
            {
                return false;
            }

            LoginRequest request = new(email, password);
            HttpResponseMessage response = await _httpClient.PostAsJsonAsync("Auth/login", request);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest or HttpStatusCode.Forbidden)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();

            LoginResponse? loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
            if (loginResponse is null || string.IsNullOrEmpty(loginResponse.Token))
            {
                return false;
            }

            await SecureStorage.Default.SetAsync(TokenKey, loginResponse.Token);

            SetToken(loginResponse.Token);
            return true;
        }

        public async Task<string?> GetTokenAsync()
        {
            if (_token is not null)
            {
                return _token;
            }

            try
            {
                string? stored = await SecureStorage.Default.GetAsync(TokenKey);
                if (!string.IsNullOrEmpty(stored))
                {
                    SetToken(stored);
                }
            }
            catch (Exception)
            {
            }

            return _token;
        }

        public async Task LogoutAsync()
        {
            SecureStorage.Default.Remove(TokenKey);
            _token = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        public void SetToken(string token)
        {
            _token = token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}