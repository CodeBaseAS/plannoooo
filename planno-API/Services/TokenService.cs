using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Models.Models;
using System.Security.Claims;
using System.Text;
using static planno_API.Dtos.AuthDtos;

namespace planno_API.Services
{
    public interface ITokenService
    {
        LoginResponse CreateToken(User user);
    }
    public class TokenService(IConfiguration configuration) : ITokenService
    {
        public LoginResponse CreateToken(User user)
        {
            SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
            SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);
            DateTime expiresAt = DateTime.UtcNow.AddHours(1);

            SecurityTokenDescriptor descriptor = new()
            {
                Subject = new ClaimsIdentity(
                [
                    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                    new Claim(JwtRegisteredClaimNames.Name, user.Username),
                    new Claim(JwtRegisteredClaimNames.Email, user.Email)
                ]),
                Issuer = configuration["Jwt:Issuer"],
                Audience = configuration["Jwt:Audience"],
                Expires = expiresAt,
                SigningCredentials = credentials
            };

            string token = new JsonWebTokenHandler().CreateToken(descriptor);
            return new LoginResponse(token, expiresAt);
        }
    }
}
