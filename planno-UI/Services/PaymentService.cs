using static Models.Dtos.BillingDtos;

namespace planno_UI.Services
{
    public class PaymentService(HttpClient httpClient)
    {
        public async Task<CheckoutResponse?> CheckoutAsync(CheckoutRequest checkoutRequest)
        {
            var response = await httpClient.PostAsJsonAsync($"Payments/checkout", checkoutRequest);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CheckoutResponse>();
        }
    }
}
