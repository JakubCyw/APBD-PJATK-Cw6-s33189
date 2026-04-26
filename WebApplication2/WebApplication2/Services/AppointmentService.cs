using WebApplication2.DTO_s;
using Microsoft.Data.SqlClient;
using System.Data;

namespace WebApplication2.Services;

public class AppointmentService : IAppointmentService
{
    private readonly string _connectionString;
    public AppointmentService(IConfiguration cfg) => _connectionString = cfg.GetConnectionString("DefaultConnection");
    
    public async Task<IEnumerable<AppointmentListDto>> GetAllAppointmentsAsync(string? status, string? patientLastName)
    {
        var list = new List<AppointmentListDto>();
        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(@"
            SELECT a.IdAppointment, a.AppointmentDate, a.Status, a.Reason, 
                p.FirstName + ' ' + p.LastName, p.Email
            From Appointments a
            JOIN Patients p ON a.IdPatient = p.IdPatient
            WHERE (@S IS NULL OR a.Status = @S) AND (@L IS NULL OR p.LastName = @L)
            ORDER BY a.AppointmentDate", conn);
        cmd.Parameters.AddWithValue("@S", (object?)status ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@L", (object?)patientLastName ?? DBNull.Value);
        
        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            list.Add(new AppointmentListDto(
                reader.GetInt32(0), 
                reader.GetDateTime(1), 
                reader.GetString(2), 
                reader.GetString(3), 
                reader.GetString(4), 
                reader.GetString(5)));
        return list;
    }

    public Task<AppointmentDetailsDto?> GetAppointmentDetailsAsync(int id)
    {
        throw new NotImplementedException();
        await using var conn = new SqlConnection(_connectionString);
        await using var cmd = new SqlCommand(@"
            SELECT a.IdAppointment, a.AppointmentDate, a.Status, a.Reason, a.InternalNotes, 
                a.CreatedAt, p.FirstName, p.LastName, p.Email, p.PhoneNumber, 
                d.FirstName, d.LastName, d.LicenseNumber
            From Appointments a
            JOIN Patients p ON a.IdPatient = p.IdPatient
            JOIN Doctors d ON a.IdDoctor = d.IdDoctor
            WHERE a.IdAppointment = @Id", conn);
        cmd.Parameters.AddWithValue("@Id", id);
    }

    public Task<int> CreateAppointmentAsync(CreateAppointmentRequestDto dto)
    {
        throw new NotImplementedException();
    }

    public Task UpdateAppointmentAsync(int id, UpdateAppointmentRequestDto dto)
    {
        throw new NotImplementedException();
    }

    public Task DeleteAppointmentAsync(int idAppointment)
    {
        throw new NotImplementedException();
    }
}