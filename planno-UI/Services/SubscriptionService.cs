using static Models.Dtos.BillingDtos;

namespace planno_UI.Services
{
    public class SubscriptionService(HttpClient httpClient)
    {
        public async Task<List<SubscriptionResponse>?> GetSubscriptionsAsync()
        {
            return await httpClient.GetFromJsonAsync<List<SubscriptionResponse>>("Subscriptions");
        }

        public async Task<SubscriptionStatusResponse?> GetSubscriptionStatusAsync()
        {
            return await httpClient.GetFromJsonAsync<SubscriptionStatusResponse>($"Subscriptions/me");
        }
    }
}
