using Microsoft.AspNetCore.Components.Authorization;
using System.Buffers.Text;
using System.Security.Claims;
using System.Text.Json;

namespace planno_UI.Services
{
    public class TokenAuthenticationStateProvider(AuthService authService) : AuthenticationStateProvider
    {
        private readonly AuthService _authService = authService;

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            string? token = await _authService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            List<Claim> claims = ReadClaims(token);
            ClaimsIdentity identity = new(claims, "jwt", "unique_name", "role");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        public void ReportStatusChange()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        private static List<Claim> ReadClaims(string token)
        {
            string payloadPart = token.Split('.')[1];
            byte[] payloadBytes = Base64Url.DecodeFromChars(payloadPart);
            JsonDocument payload = JsonDocument.Parse(payloadBytes);

            List<Claim> claims = [];
            foreach (JsonProperty property in payload.RootElement.EnumerateObject())
            {
                claims.Add(new Claim(property.Name, property.Value.ToString()));
            }

            return claims;
        }
    }
}
