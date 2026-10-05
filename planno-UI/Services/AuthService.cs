using static Models.Dtos.AuthDtos;

namespace planno_UI.Services
{
    public class AuthService(HttpClient httpClient)
    {
        public async Task RegisterAsync(RegisterRequest registerRequest)
        {
            var response = await httpClient.PostAsJsonAsync("Auth/register", registerRequest); 
            
            response.EnsureSuccessStatusCode();
        }

        public async Task<LoginResponse?> LoginAsync(LoginRequest loginRequest)
        {
            var response = await httpClient.PostAsJsonAsync("Auth/login", loginRequest);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<LoginResponse>();
        }
    }
}
