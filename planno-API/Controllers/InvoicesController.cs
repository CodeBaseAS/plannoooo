using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using planno_API.Services;
using System.Security.Claims;
using static planno_API.Dtos.BillingDtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace planno_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class InvoicesController(PlannoDbContext db, InvoiceFileStore files) : ControllerBase
    {
        private int CurrentUserId => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

        [HttpGet]
        public async Task<List<InvoiceResponse>> GetMine() =>
            await db.Invoices
                .Where(i => i.Payment.UserId == CurrentUserId)
                .OrderByDescending(i => i.IssuedAt)
                .Select(i => new InvoiceResponse(
                    i.Id, i.InvoiceNumber, i.IssuedAt, i.Payment.Amount,
                    i.Payment.Subscription.Description, i.PdfUrl))
                .ToListAsync();

        [HttpGet("{invoiceNumber}/pdf")]
        public async Task<IActionResult> DownloadPdf(string invoiceNumber)
        {
            bool owned = await db.Invoices.AnyAsync(i => i.InvoiceNumber == invoiceNumber && i.Payment.UserId == CurrentUserId);
            if (!owned)
            {
                return NotFound();
            }

            string path = files.PathFor(invoiceNumber);
            if (!System.IO.File.Exists(path))
            {
                return NotFound();
            }

            return PhysicalFile(path, "application/pdf", $"{invoiceNumber}.pdf");
        }
    }
}
