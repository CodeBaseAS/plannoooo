using Microsoft.AspNetCore.Components.Authorization;
using planno_MAUI.Services;

namespace planno_MAUI;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        builder.Services.AddHttpClient("PlannoApi", client =>
        {
            client.BaseAddress = new Uri("https://10.0.2.2:7067/api/");
        }).ConfigurePrimaryHttpMessageHandler(() =>
        {
            var handler = new HttpClientHandler();
            
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            
            return handler;
        });

        builder.Services.AddScoped(serviceProvider => serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("PlannoApi"));

        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<AppointmentService>();
        builder.Services.AddScoped<InvoiceService>();
        builder.Services.AddScoped<PaymentService>();
        builder.Services.AddScoped<SubscriptionService>();

        builder.Services.AddCascadingAuthenticationState();

        builder.Services.AddAuthorizationCore();

        builder.Services.AddScoped<TokenAuthenticationStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(serviceProvider => serviceProvider.GetRequiredService<TokenAuthenticationStateProvider>());

        return builder.Build();
    }
}