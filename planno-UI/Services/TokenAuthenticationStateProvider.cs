using Microsoft.AspNetCore.Components.Authorization;
using System.Buffers.Text;
using System.Security.Claims;
using System.Text.Json;

namespace planno_UI.Services
{
    public class TokenAuthenticationStateProvider(AuthService authService) : AuthenticationStateProvider
    {
        private static readonly AuthenticationState Anonymous = new(new ClaimsPrincipal(new ClaimsIdentity()));

        private readonly AuthService _authService = authService;

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string? token = await _authService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return Anonymous;
            }

            List<Claim>? claims = TryReadClaims(token);
            if (claims is null || IsExpired(claims))
            {
                // Ungültiger oder abgelaufener Token -> aufräumen
                await _authService.LogoutAsync();
                return Anonymous;
            }

            ClaimsIdentity identity = new(claims, "jwt", "unique_name", "role");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        /// <summary>Nach Login/Logout aufrufen, damit AuthorizeView & Co. neu auswerten.</summary>
        public void ReportStatusChange()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        private static List<Claim>? TryReadClaims(string token)
        {
            string[] parts = token.Split('.');
            if (parts.Length != 3)
            {
                return null;
            }

            try
            {
                byte[] payloadBytes = Base64Url.DecodeFromChars(parts[1]);
                using JsonDocument payload = JsonDocument.Parse(payloadBytes);

                List<Claim> claims = [];
                foreach (JsonProperty property in payload.RootElement.EnumerateObject())
                {
                    // Arrays (z. B. mehrere Rollen) als einzelne Claims ablegen
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement item in property.Value.EnumerateArray())
                        {
                            claims.Add(new Claim(property.Name, item.ToString()));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(property.Name, property.Value.ToString()));
                    }
                }

                return claims;
            }
            catch (Exception ex) when (ex is FormatException or JsonException or InvalidOperationException)
            {
                return null;
            }
        }

        private static bool IsExpired(List<Claim> claims)
        {
            Claim? exp = claims.FirstOrDefault(c => c.Type == "exp");
            return exp is not null
                && long.TryParse(exp.Value, out long seconds)
                && DateTimeOffset.FromUnixTimeSeconds(seconds) <= DateTimeOffset.UtcNow;
        }
    }
}
