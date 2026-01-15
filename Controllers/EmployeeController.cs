using HospIntel.EmployeeService.Models;
using HospIntel.EmployeeService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

[ApiController]
[Route("api/employee")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _service;

    public EmployeeController(IEmployeeService service)
    {
        _service = service;
    }

    // API_102_Employee_Registration
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] Employee employee, CancellationToken cancellationToken)
    {
        try
        {
            await _service.RegisterEmployeeAsync(employee, cancellationToken).ConfigureAwait(false);

            // If stored proc returned new id, return Created
            if (employee.EmpId > 0)
            {
                return CreatedAtAction(nameof(Get), new { empId = employee.EmpId }, employee);
            }

            return Ok(new { message = "Employee Registered Successfully" });
        }
        catch (ApplicationException ex) when (ex.InnerException is SqlException sqlEx)
        {
            // Handle known SQL errors from stored procedures (PAN/Aadhar validation etc.)
            if (sqlEx.Number == 51000 || sqlEx.Number == 51001 || sqlEx.Number == 51002)
            {
                return BadRequest(new { error = sqlEx.Message, code = sqlEx.Number });
            }

            // For other SQL errors return generic DB error
            return StatusCode(500, new { error = sqlEx.Message, code = sqlEx.Number });
        }
        catch (ApplicationException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    // API_103_Employee_View (By ID)
    [HttpGet("{empId}")]
    public async Task<IActionResult> Get(int empId, CancellationToken cancellationToken)
    {
        var emp = await _service.GetEmployeeByIdAsync(empId, cancellationToken).ConfigureAwait(false);
        return emp == null ? NotFound() : Ok(emp);
    }

    // View All Employees with paging and filters
    [HttpGet("all")]
    public async Task<IActionResult> GetAll([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 100, [FromQuery] string? department = null, [FromQuery(Name = "availabilityStatus")] string? availabilityStatus = null, CancellationToken cancellationToken = default)
    {
        var list = await _service.GetAllEmployeesAsync(pageNumber, pageSize, department, availabilityStatus, cancellationToken).ConfigureAwait(false);
        return Ok(list);
    }

    // API_104_Employee_Maintenance (Update)
    [HttpPut("update")]
    public async Task<IActionResult> Update([FromBody] Employee employee, CancellationToken cancellationToken)
    {
        await _service.UpdateEmployeeAsync(employee, cancellationToken).ConfigureAwait(false);
        return Ok("Employee Updated");
    }

    // Delete Employee
    [HttpDelete("{empId}")]
    public async Task<IActionResult> Delete(int empId, CancellationToken cancellationToken)
    {
        await _service.DeleteEmployeeAsync(empId, cancellationToken).ConfigureAwait(false);
        return Ok("Employee Deleted");
    }
}
