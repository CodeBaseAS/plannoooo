using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Models.Models;
using System.Security.Claims;
using static Models.Dtos.BillingDtos;
using Microsoft.IdentityModel.JsonWebTokens;

namespace planno_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubscriptionsController(PlannoDbContext db) : ControllerBase
    {
        private int CurrentUserId => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

        [HttpGet]
        public async Task<List<SubscriptionResponse>> GetAll()
        {
            return await db.Subscriptions.OrderBy(s => s.Price).Select(s => new SubscriptionResponse(s.Id, s.Description, s.Price, s.Interval)).ToListAsync();
        }

        [Authorize]
        [HttpGet("me")]
        public async Task<ActionResult<SubscriptionStatusResponse>> GetMine()
        {
            User? user = await db.Users.Include(u => u.ActiveSubscription).FirstOrDefaultAsync(u => u.Id == CurrentUserId);
            if (user is null)
            {
                return Unauthorized();
            }

            bool premium = user.ActiveSubscriptionId is not null && user.SubscriptionExpiresAt > DateTime.UtcNow;

            return new SubscriptionStatusResponse(
                premium,
                premium ? user.ActiveSubscriptionId : null,
                premium ? user.ActiveSubscription!.Description : null,
                user.SubscriptionExpiresAt);
        }
    }
}
