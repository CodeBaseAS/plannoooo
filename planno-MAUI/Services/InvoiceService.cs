using System.Net.Http.Json;
using static Models.Dtos.BillingDtos;

namespace planno_MAUI.Services
{
    public class InvoiceService(HttpClient httpClient)
    {
        private readonly HttpClient _httpClient = httpClient;

        public async Task<List<InvoiceResponse>?> GetInvoicesAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<InvoiceResponse>>("Invoices");
        }

        public async Task<byte[]?> DownloadPdfAsync(string invoiceNumber)
        {
            var response = await _httpClient.GetAsync($"Invoices/{invoiceNumber}/pdf");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadAsByteArrayAsync();
        }
    }
}
