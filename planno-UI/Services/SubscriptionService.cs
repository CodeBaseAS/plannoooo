using static Models.Dtos.BillingDtos;

namespace planno_UI.Services
{
    public class SubscriptionService(HttpClient httpClient)
    {
        private readonly HttpClient _httpClient = httpClient;

        public async Task<List<SubscriptionResponse>?> GetSubscriptionsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<SubscriptionResponse>>("Subscriptions");
        }

        public async Task<SubscriptionStatusResponse?> GetSubscriptionStatusAsync()
        {
            return await _httpClient.GetFromJsonAsync<SubscriptionStatusResponse>($"Subscriptions/me");
        }
    }
}
