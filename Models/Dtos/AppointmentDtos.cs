using System.ComponentModel.DataAnnotations;

namespace Models.Dtos
{
    public class AppointmentDtos
    {
        public record AppointmentRequest(string Title, DateTime StartTime, DateTime EndTime, string? Location, bool IsAllDay) : IValidatableObject
        {
            public IEnumerable<ValidationResult> Validate(ValidationContext context)
            {
                if (EndTime < StartTime)
                {
                    yield return new ValidationResult("\r\nThe end must not come before the start.", [nameof(EndTime)]);
                }
            }
        }

        public record AppointmentResponse(int Id, string Title, DateTime StartTime, DateTime EndTime, string? Location, bool IsAllDay);
    }
}
