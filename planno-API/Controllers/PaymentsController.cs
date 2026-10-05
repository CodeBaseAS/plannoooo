using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models.Models;
using planno_API.Services;
using System.Security.Claims;
using static Models.Dtos.BillingDtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace planno_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PaymentsController(PlannoDbContext db, IInvoicePdfGenerator pdfGenerator, InvoiceFileStore files) : ControllerBase
    {
        private int CurrentUserId => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

        [HttpPost("checkout")]
        public async Task<ActionResult<CheckoutResponse>> Checkout(CheckoutRequest request)
        {
            Subscription? subscription = await db.Subscriptions.FindAsync(request.SubscriptionId);
            if (subscription is null)
            {
                return NotFound("Subscription not found.");
            }

            User? user = await db.Users.FindAsync(CurrentUserId);
            if (user is null)
            {
                return Unauthorized();
            }

            string number = request.CardNumber.Replace(" ", "");

            string brand = number[0] switch { '4' => "Visa", '5' => "Mastercard", '3' => "American Express", _ => "Unknown" };
            string last4 = number[^4..];
            DateTime cardExpiresAt = new DateTime(request.ExpiryYear, request.ExpiryMonth, 1).AddMonths(1).AddSeconds(-1);

            CardDetails card = await db.CardDetails
                .FirstOrDefaultAsync(c => c.UserId == user.Id && c.CardBrand == brand && c.CardLast4 == last4 && c.ExpiresAt == cardExpiresAt) 
                ?? new CardDetails 
                { 
                    UserId = user.Id, 
                    CardBrand = brand, 
                    CardLast4 = last4, 
                    ExpiresAt = cardExpiresAt 
                };

            DateTime now = DateTime.UtcNow;

            DateTime periodStart = user.ActiveSubscriptionId is not null && user.SubscriptionExpiresAt > now ? user.SubscriptionExpiresAt.Value : now;
            DateTime periodEnd = periodStart.AddMonths(subscription.Interval);

            await using var transaction = await db.Database.BeginTransactionAsync();

            Payment payment = new()
            {
                Card = card,
                SubscriptionId = subscription.Id,
                UserId = user.Id,
                PaidAt = now,
                Amount = subscription.Price
            };
            db.Payments.Add(payment);

            user.ActiveSubscriptionId = subscription.Id;
            user.SubscriptionExpiresAt = periodEnd;
            await db.SaveChangesAsync();

            string invoiceNumber = $"RE-{now:yyyy}-{payment.Id:D6}";
            string invoiceUrl = $"/api/invoices/{invoiceNumber}/pdf";

            try
            {
                byte[] pdf = pdfGenerator.Generate(new InvoiceData(invoiceNumber, now, user.Username, user.Email, subscription.Description, periodStart, periodEnd, subscription.Price, brand, last4));
                await files.SaveAsync(invoiceNumber, pdf);

                db.Invoices.Add(new Invoice
                {
                    PaymentId = payment.Id,
                    InvoiceNumber = invoiceNumber,
                    IssuedAt = now,
                    PdfUrl = invoiceUrl
                });
                await db.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                files.Delete(invoiceNumber);
                throw;
            }

            return Ok(new CheckoutResponse(payment.Id, payment.Amount, now, periodEnd, invoiceNumber, invoiceUrl));
        }
    }
}
