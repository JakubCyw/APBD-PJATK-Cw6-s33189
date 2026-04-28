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
        {
            list.Add(new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                PatientFullName = reader.IsDBNull(4) ? "" : reader.GetString(4),
                PatientEmail = reader.IsDBNull(4) ? "" : reader.GetString(5)
            });
        }
            
        return list;
    }

    public async Task<AppointmentDetailsDto?> GetAppointmentDetailsAsync(int id)
    {
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
        
        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        return new AppointmentDetailsDto
        {
            IdAppointment = reader.GetInt32(0),
            AppointmentDate = reader.GetDateTime(1),
            Status = reader.GetString(2),
            Reason = reader.GetString(3),
            InternalNotes = reader.GetString(4),
            CreatedAt = reader.GetDateTime(5),
            PatientFirstName = reader.IsDBNull(6) ? "" : reader.GetString(6),
            PatientLastName = reader.IsDBNull(7) ? "" : reader.GetString(7),
            PatientEmail = reader.IsDBNull(8) ? "" : reader.GetString(8),
            PatientPhoneNumber = reader.IsDBNull(9) ? "" : reader.GetString(9),
            DoctorFirstName = reader.IsDBNull(10) ? "" : reader.GetString(10),
            DoctorLastName = reader.IsDBNull(11) ? "" : reader.GetString(11),
            DoctorLicenseNumber = reader.IsDBNull(12) ? "" : reader.GetString(12)
        };
    }

    public async Task<int> CreateAppointmentAsync(CreateAppointmentRequestDto dto)
    {
        if (dto.AppointmentDate < DateTime.Now) throw new ArgumentException("Data nie moze byc z przyszłości");
        
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var conflictCmd =
            new SqlCommand(
                "SELECT COUNT(1) FROM Appointments WHERE " +
                "IdDoctor = @D AND AppointmentDate = @Date AND Status = 'Scheduled'",
                conn);
        conflictCmd.Parameters.AddWithValue("@D", dto.IdDoctor);
        conflictCmd.Parameters.AddWithValue("@Date", dto.AppointmentDate);
        if ((int)await conflictCmd.ExecuteScalarAsync()! > 0) throw new InvalidOperationException("Conflict");
        
        var cmd = new SqlCommand(@"
            INSERT INTO Appointments (IdPatient, IdDoctor, AppointmentDate, Status, Reason, CreatedAt)
            VALUES (@P, @D, @Date, 'Scheduled' @R, SYSUTCDATETIME());
            SELECT CAST(scope_identity() AS int);", conn);
        cmd.Parameters.AddWithValue("@P", dto.IdPatient);
        cmd.Parameters.AddWithValue("@D", dto.IdDoctor);
        cmd.Parameters.AddWithValue("@Date", dto.AppointmentDate);
        cmd.Parameters.AddWithValue("@R", dto.Reason);

        return (int)await cmd.ExecuteScalarAsync();
    }

    public async Task UpdateAppointmentAsync(int id, UpdateAppointmentRequestDto dto)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var checkCmd = new SqlCommand("SELECT Status FROM Appointments WHERE IdAppointment = @Id", conn);
        checkCmd.Parameters.AddWithValue("@Id", id);
        var currentStatus = await checkCmd.ExecuteScalarAsync();
        if (currentStatus == null) throw new KeyNotFoundException();
        if (currentStatus.ToString() == "Completed") throw new InvalidOperationException("COMPLETED_NO_CHANGE");

        var updateCmd = new SqlCommand(@"
            UPDATE Appointments SET IdPatient=@P, IdDoctor=@D, AppointmentDate=@Dt, Status=@S, Reason=@R, InternalNotes=@N
            WHERE IdAppointment=@Id", conn);
        updateCmd.Parameters.AddWithValue("@P", dto.IdPatient);
        updateCmd.Parameters.AddWithValue("@D", dto.IdDoctor);
        updateCmd.Parameters.AddWithValue("@Dt", dto.AppointmentDate);
        updateCmd.Parameters.AddWithValue("@S", dto.Status);
        updateCmd.Parameters.AddWithValue("@R", dto.Reason);
        updateCmd.Parameters.AddWithValue("@N", (object?)dto.InternalNotes ?? DBNull.Value);
        updateCmd.Parameters.AddWithValue("@Id", id);

        await updateCmd.ExecuteNonQueryAsync();
    }

    public async Task DeleteAppointmentAsync(int id)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();

        var checkCmd = new SqlCommand("SELECT Status FROM Appointments WHERE IdAppointment = @Id", conn);
        checkCmd.Parameters.AddWithValue("@Id", id);
        var status = await checkCmd.ExecuteScalarAsync();

        if (status == null) throw new KeyNotFoundException();
        if (status.ToString() == "Completed") throw new InvalidOperationException("COMPLETED_NO_DELETE");

        var deleteCmd = new SqlCommand("DELETE FROM Appointments WHERE IdAppointment = @Id", conn);
        deleteCmd.Parameters.AddWithValue("@Id", id);
        await deleteCmd.ExecuteNonQueryAsync();
    }
}