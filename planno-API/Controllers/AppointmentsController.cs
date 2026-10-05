using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using static Models.Dtos.AppointmentDtos;
using Models.Models;

namespace planno_API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AppointmentsController(PlannoDbContext db) : ControllerBase
    {
        private int CurrentUserId => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

        private IQueryable<Appointment> QueryUserAppointments() => db.Appointments.Where(a => db.UserAppointments.Any(ua => ua.AppointmentId == a.Id && ua.UserId == CurrentUserId));

        private static AppointmentResponse ToResponse(Appointment a) => new(a.Id, a.Title, a.StartTime, a.EndTime, a.Location, a.IsAllDay);

        [HttpGet]
        public async Task<ActionResult<List<AppointmentResponse>>> GetAll()
        {
            List<Appointment> list = await QueryUserAppointments().OrderBy(a => a.StartTime).ToListAsync();

            return list.Select(ToResponse).ToList();
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<AppointmentResponse>> GetById(int id)
        {
            Appointment? appointment = await QueryUserAppointments().FirstOrDefaultAsync(a => a.Id == id);

            return appointment is null ? NotFound() : ToResponse(appointment);
        }

        [HttpPost]
        public async Task<ActionResult<AppointmentResponse>> Create(AppointmentRequest request)
        {
            Appointment appointment = new()
            {
                Title = request.Title,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Location = request.Location,
                IsAllDay = request.IsAllDay
            };

            db.UserAppointments.Add(new UserAppointment
            {
                UserId = CurrentUserId,
                Appointment = appointment
            });
            await db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, ToResponse(appointment));
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<AppointmentResponse>> Update(int id, AppointmentRequest request)
        {
            Appointment? appointment = await QueryUserAppointments().FirstOrDefaultAsync(a => a.Id == id);
            if (appointment is null)
            {
                return NotFound();
            }

            appointment.Title = request.Title;
            appointment.StartTime = request.StartTime;
            appointment.EndTime = request.EndTime;
            appointment.Location = request.Location;
            appointment.IsAllDay = request.IsAllDay;

            await db.SaveChangesAsync();
            return ToResponse(appointment);
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            UserAppointment? link = await db.UserAppointments.FirstOrDefaultAsync(ua => ua.AppointmentId == id && ua.UserId == CurrentUserId);
            if (link is null)
            {
                return NotFound();
            }

            db.UserAppointments.Remove(link);

            await db.SaveChangesAsync();
            return NoContent();
        }
    }
}
