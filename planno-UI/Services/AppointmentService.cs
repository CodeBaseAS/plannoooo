using Models.Models;
using static Models.Dtos.AppointmentDtos;

namespace planno_UI.Services
{
    public class AppointmentService(HttpClient httpClient)
    {
        private readonly HttpClient _httpClient = httpClient;

        public async Task<List<AppointmentResponse>?> GetAppointmentListAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<AppointmentResponse>>("Appointments");
        }
       
        public async Task<AppointmentResponse?> GetAppointmentByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<AppointmentResponse>($"Appointments/{id}");
        }
       
        public async Task<AppointmentResponse?> PostAppointmentAsync(AppointmentRequest appointmentRequest)
        {
            var response = await _httpClient.PostAsJsonAsync("Appointments", appointmentRequest);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        }
       
        public async Task<AppointmentResponse?> UpdateAppointmentAsync(int id, AppointmentRequest appointmentRequest)
        {
            var response = await _httpClient.PatchAsJsonAsync($"Appointments/{id}", appointmentRequest);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            return await response.Content.ReadFromJsonAsync<AppointmentResponse>();
        }

        public async Task DeleteAppointmentAsync(int id)
        {
            await _httpClient.DeleteAsync($"Appointments/{id}");
        }
    }
}
