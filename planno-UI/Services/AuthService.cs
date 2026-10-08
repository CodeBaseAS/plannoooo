using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using static Models.Dtos.AuthDtos;

namespace planno_UI.Services
{
    public class AuthService(HttpClient httpClient, ProtectedLocalStorage protectedLocalStorage)
    {
        private const string TokenKey = "zugangsToken";
        private readonly HttpClient _httpClient = httpClient;
        private readonly ProtectedLocalStorage _protectedLocalStorage = protectedLocalStorage;
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

            await _protectedLocalStorage.SetAsync(TokenKey, loginResponse.Token);

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
                string? stored = await ReadAsync(_protectedLocalStorage);
                if (!string.IsNullOrEmpty(stored))
                {
                    SetToken(stored);
                }
            }
            catch (InvalidOperationException)
            {
                // Prerendering: Kein JS-Interop möglich
            }
            catch (JSDisconnectedException)
            {
                // Verbindung zum Browser getrennt
            }

            return _token;
        }

        public async Task LogoutAsync()
        {
            await _protectedLocalStorage.DeleteAsync(TokenKey);
            _token = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        public void SetToken(string token)
        {
            _token = token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private static async Task<string?> ReadAsync(ProtectedBrowserStorage storage)
        {
            try
            {
                ProtectedBrowserStorageResult<string> outcome = await storage.GetAsync<string>(TokenKey);
                return outcome.Success && !string.IsNullOrEmpty(outcome.Value) ? outcome.Value : null;
            }
            catch (CryptographicException)
            {
                await storage.DeleteAsync(TokenKey);
                return null;
            }
        }
    }
}