using WebApplication2.DTO_s;

namespace WebApplication2.Services;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAppointmentsAsync(string? status, string? patientLastName);
    Task<AppointmentDetailsDto?> GetAppointmentDetailsAsync(int id);
    Task<int> CreateAppointmentAsync(CreateAppointmentRequestDto dto);
    Task UpdateAppointmentAsync(int id, UpdateAppointmentRequestDto dto);
    Task DeleteAppointmentAsync(int id);
}