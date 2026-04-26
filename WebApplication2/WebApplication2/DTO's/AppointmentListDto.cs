namespace WebApplication2.DTO_s;

public record AppointmentListDto(
    int IdAppointment,
    DateTime AppointmentDate,
    string Status,
    string Reason,
    string PatientFullName,
    string PatientEmail
    );
