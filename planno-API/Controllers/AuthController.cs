using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using planno_API.Models;
using planno_API.Services;
using static planno_API.Dtos.AuthDtos;

namespace planno_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController(PlannoDbContext db, ITokenService tokenService, IPasswordHasher<User> passwordHasher) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            bool exists = await db.Users.AnyAsync(u => u.Email == request.Email || u.Username == request.Username);

            if (exists)
            {
                return Conflict("User with this email or username already exsits.");
            }

            User user = new()
            {
                Username = request.Username,
                Email = request.Email
            };
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

            db.Users.Add(user);
            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(Register), new { user.Id }, new { user.Id, user.Username });
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            User? user = await db.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user is null) return Unauthorized("Invalid login credentials.");

            PasswordVerificationResult result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

            if (result == PasswordVerificationResult.Failed) return Unauthorized("Invalid login credentials.");

            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
                await db.SaveChangesAsync();
            }

            return Ok(tokenService.CreateToken(user));
        }
    }
}
