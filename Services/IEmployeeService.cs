using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HospIntel.EmployeeService.Models;

namespace HospIntel.EmployeeService.Services
{
    public interface IEmployeeService
    {
        Task RegisterEmployeeAsync(Employee employee, CancellationToken cancellationToken = default);
        Task<Employee?> GetEmployeeByIdAsync(int empId, CancellationToken cancellationToken = default);
        Task<List<Employee>> GetAllEmployeesAsync(int pageNumber = 1, int pageSize = 100, string department = null, string availabilityStatus = null, CancellationToken cancellationToken = default);
        Task UpdateEmployeeAsync(Employee employee, CancellationToken cancellationToken = default);
        Task DeleteEmployeeAsync(int empId, CancellationToken cancellationToken = default);
    }
}
