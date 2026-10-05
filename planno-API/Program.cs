
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Models.Models;
using planno_API.Services;
using QuestPDF.Infrastructure;
using System.Text;

namespace planno_API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var builder = WebApplication.CreateBuilder(args);
            var connectionString = builder.Configuration.GetConnectionString("plannoDbContext") ?? throw new InvalidOperationException("Connection string 'plannoDbContext' not found.");

            builder.Services.AddDbContext<PlannoDbContext>(options => options.UseSqlServer(connectionString));

            builder.Services.AddSingleton<ITokenService, TokenService>();
            builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
            builder.Services.AddSingleton<IInvoicePdfGenerator, InvoicePdfGenerator>();
            builder.Services.AddSingleton<InvoiceFileStore>();

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();

            string jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key is missing.");

            builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        var h = ctx.Request.Headers.Authorization.ToString();
                        Console.WriteLine($"{ctx.Request.Method} {ctx.Request.Scheme}://{ctx.Request.Host}{ctx.Request.Path} | AuthLen={h.Length} | Start='{(h.Length > 15 ? h[..15] : h)}' | Ende='{(h.Length > 10 ? h[^10..] : h)}'");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = ctx =>
                    {
                        Console.WriteLine("Token OK");
                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = ctx =>
                    {
                        Console.WriteLine("Auth failed: " + ctx.Exception);
                        return Task.CompletedTask;
                    },
                    OnChallenge = ctx =>
                    {
                        Console.WriteLine($"Challenge: {ctx.Error} - {ctx.ErrorDescription} | AuthenticateFailure: {ctx.AuthenticateFailure?.Message}");
                        return Task.CompletedTask;
                    }
                };
            });

            builder.Services.AddAuthorization();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
