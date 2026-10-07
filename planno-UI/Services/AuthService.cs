using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using Microsoft.JSInterop;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using static Models.Dtos.AuthDtos;

namespace planno_UI.Services
{
    public class AuthService(
        HttpClient httpClient,
        ProtectedLocalStorage protectedLocalStorage,
        ProtectedSessionStorage protectedSessionStorage)
    {
        private const string TokenKey = "zugangsToken";
        private readonly HttpClient _httpClient = httpClient;
        private readonly ProtectedLocalStorage _protectedLocalStorage = protectedLocalStorage;
        private readonly ProtectedSessionStorage _protectedSessionStorage = protectedSessionStorage;
        private string? _token;

        public async Task RegisterAsync(RegisterRequest registerRequest)
        {
            var response = await _httpClient.PostAsJsonAsync("Auth/register", registerRequest);

            response.EnsureSuccessStatusCode();
        }

        /// <param name="rememberMe">
        /// true: Token bleibt auch nach dem Schließen des Browsers erhalten (LocalStorage),
        /// false: Token gilt nur für diese Browser-Sitzung (SessionStorage).
        /// </param>
        /// <returns>false bei falschen Zugangsdaten; andere Fehler (Server down, 5xx) lösen eine HttpRequestException aus.</returns>
        public async Task<bool> LoginAsync(string email, string password, bool rememberMe = true)
        {
            // Vorher war die Bedingung umgekehrt (!IsNullOrEmpty) – jede gültige E-Mail wurde abgelehnt.
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

            // Token nur an einer Stelle ablegen
            if (rememberMe)
            {
                await _protectedLocalStorage.SetAsync(TokenKey, loginResponse.Token);
                await _protectedSessionStorage.DeleteAsync(TokenKey);
            }
            else
            {
                await _protectedSessionStorage.SetAsync(TokenKey, loginResponse.Token);
                await _protectedLocalStorage.DeleteAsync(TokenKey);
            }

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
                string? stored = await ReadAsync(_protectedLocalStorage) ?? await ReadAsync(_protectedSessionStorage);
                if (!string.IsNullOrEmpty(stored))
                {
                    SetToken(stored);
                }
            }
            catch (InvalidOperationException)
            {
                // Beim Prerendering ist noch kein JS-Interop möglich -> vorerst „nicht angemeldet“.
                // Nichts cachen, der nächste Aufruf (interaktiv) liest den Token dann wirklich.
            }
            catch (JSDisconnectedException)
            {
                // Verbindung zum Browser ist weg
            }

            return _token;
        }

        public async Task LogoutAsync()
        {
            await _protectedLocalStorage.DeleteAsync(TokenKey);
            await _protectedSessionStorage.DeleteAsync(TokenKey);
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
                // Eintrag nicht mehr entschlüsselbar (z. B. neue Data-Protection-Keys) -> verwerfen
                await storage.DeleteAsync(TokenKey);
                return null;
            }
        }
    }
}
