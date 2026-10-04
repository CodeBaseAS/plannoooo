using System.ComponentModel.DataAnnotations;

namespace planno_API.Dtos
{
    public class AuthDtos
    {
        public record RegisterRequest(string Username, string Email, string Password);
        public record LoginRequest(string Email, string Password);
        public record LoginResponse(string Token, DateTime ExpiresAt);
    }
}
