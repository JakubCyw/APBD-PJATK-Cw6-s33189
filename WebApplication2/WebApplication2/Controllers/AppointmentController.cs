using Microsoft.AspNetCore.Mvc;
using WebApplication2.DTO_s;
using WebApplication2.Services;

namespace WebApplication2.Controllers;

[ApiController]
[Route("api/appointments")]
public class AppointmentController : ControllerBase
{
    private readonly IAppointmentService _service;
    public AppointmentController(IAppointmentService service)
    {
        _service = service;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status,  [FromQuery] string? patientFullName)
        => Ok(await _service.GetAllAppointmentsAsync(status, patientFullName));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var app = await _service.GetAppointmentDetailsAsync(id);
        if (app == null) return NotFound();
        return Ok(app);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateAppointmentRequestDto dto)
    {
        try
        {
            var id = await _service.CreateAppointmentAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id }, new { id });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponseDto { Message = ex.Message });
        }
        catch (InvalidOperationException)
        {
            return Conflict(new ErrorResponseDto {Message = "Termin lekarza jest juz zajety"});
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateAppointmentRequestDto dto)
    {
        try
        {
            await _service.UpdateAppointmentAsync(id, dto);
            return Ok();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponseDto {Message = ex.Message});
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAppointmentAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponseDto {Message = ex.Message});
        }
    }
}