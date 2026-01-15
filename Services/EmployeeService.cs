using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using HospIntel.EmployeeService.Data;
using HospIntel.EmployeeService.Models;

namespace HospIntel.EmployeeService.Services
{
    public class EmployeeService : IEmployeeService
    {
        private readonly EmployeeDbContext _context;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(EmployeeDbContext context, ILogger<EmployeeService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task RegisterEmployeeAsync(Employee employee, CancellationToken cancellationToken = default)
        {
            string? procName = null;
            try
            {
                using var conn = _context.Database.GetDbConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "usp_EMP_Create";
                procName = cmd.CommandText;
                cmd.CommandType = CommandType.StoredProcedure;

                // Match stored procedure parameter names exactly
                cmd.Parameters.Add(new SqlParameter("@EMP_Name", (object)employee.EmpName ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Date_of_Birth", employee.DateOfBirth.HasValue ? (object)employee.DateOfBirth.Value : DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Place", (object)employee.Place ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@PANNum", (object)employee.PANNum ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Aadhar", (object)employee.Aadhar ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Mobile_Num", (object)employee.MobileNum ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Department", (object)employee.Department ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Specialization", (object)employee.Specialization ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Availability_Status", (object)employee.AvailabilityStatus ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Emp_Status", (object)employee.EmpStatus ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Joining_Date", employee.JoiningDate.HasValue ? (object)employee.JoiningDate.Value : DBNull.Value));

                var outId = new SqlParameter("@New_EMP_ID", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(outId);

                if (conn.State != ConnectionState.Open) await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

                if (outId.Value != DBNull.Value && int.TryParse(outId.Value.ToString(), out var id))
                {
                    employee.EmpId = id;
                }
            }
            catch (SqlException ex)
            {
                try
                {
                    if (!string.IsNullOrEmpty(procName) && !ex.Data.Contains("Procedure"))
                    {
                        ex.Data["Procedure"] = procName;
                    }
                }
                catch { }

                _logger?.LogError(ex, "Database error while creating employee. Proc:{Proc}", procName);
                // Wrap SQL exceptions to give a clearer message for the caller / controller
                throw new ApplicationException("Database error while creating employee.", ex);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Employee?> GetEmployeeByIdAsync(int empId, CancellationToken cancellationToken = default)
        {
            string? procName = null;
            try
            {
                using var conn = _context.Database.GetDbConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "usp_EMP_GetById";
                procName = cmd.CommandText;
                cmd.CommandType = CommandType.StoredProcedure;

                var p = cmd.CreateParameter();
                p.ParameterName = "@EmpId";
                p.Value = empId;
                cmd.Parameters.Add(p);

                if (conn.State != ConnectionState.Open) await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        return MapEmployee(reader);
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error executing stored procedure {Proc} for EmpId={EmpId}", cmd.CommandText, empId);
                    throw new ApplicationException($"Error executing stored procedure {cmd.CommandText}: {ex.Message}", ex);
                }
            }
            catch (SqlException ex)
            {
                try
                {
                    if (!string.IsNullOrEmpty(procName) && !ex.Data.Contains("Procedure"))
                    {
                        ex.Data["Procedure"] = procName;
                    }
                }
                catch { }

                _logger?.LogError(ex, "Database error while fetching employee with id {EmpId}. Proc:{Proc}", empId, procName);
                throw new ApplicationException($"Database error while fetching employee with id {empId}.", ex);
            }
        }

        public async Task<List<Employee>> GetAllEmployeesAsync(int pageNumber = 1, int pageSize = 100, string department = null, string availabilityStatus = null, CancellationToken cancellationToken = default)
        {
            string? procName = null;
            try
            {
                var list = new List<Employee>();

                using var conn = _context.Database.GetDbConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "usp_EMP_GetAll";
                procName = cmd.CommandText;
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@PageNumber", pageNumber));
                cmd.Parameters.Add(new SqlParameter("@PageSize", pageSize));
                cmd.Parameters.Add(new SqlParameter("@Department", (object)department ?? DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Availability_Status", (object)availabilityStatus ?? DBNull.Value));



                if (conn.State != ConnectionState.Open) await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        list.Add(MapEmployee(reader));
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error executing stored procedure {Proc} while fetching all employees.", cmd.CommandText);
                    throw new ApplicationException($"Error executing stored procedure {cmd.CommandText}: {ex.Message}", ex);
                }

                return list;
            }
            catch (SqlException ex)
            {
                try
                {
                    if (!string.IsNullOrEmpty(procName) && !ex.Data.Contains("Procedure"))
                    {
                        ex.Data["Procedure"] = procName;
                    }
                }
                catch { }

                _logger?.LogError(ex, "Database error while fetching employees. Proc:{Proc}", procName);
                throw new ApplicationException("Database error while fetching employees.", ex);
            }

        }

        private static Employee MapEmployee(System.Data.Common.DbDataReader reader)
        {
            // Try multiple possible column names (different casing/aliases) and return first match
            object GetValueByNames(params string[] names)
            {
                foreach (var name in names)
                {
                    try
                    {
                        var ord = reader.GetOrdinal(name);
                        if (!reader.IsDBNull(ord)) return reader.GetValue(ord);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        // ignore and try next name
                    }
                }

                return null;
            }

            var emp = new Employee();

            var v = GetValueByNames("EMP_ID", "EmpId", "EMPId", "Id");
            if (v != null && int.TryParse(v.ToString(), out var id)) emp.EmpId = id;

            v = GetValueByNames("EMP_Name", "EmpName", "EMPName", "Name");
            emp.EmpName = v?.ToString();

            v = GetValueByNames("Date_of_Birth", "DateOfBirth", "DOB");
            if (v != null && DateTime.TryParse(v.ToString(), out var dob)) emp.DateOfBirth = dob;

            v = GetValueByNames("Place", "City", "Location");
            emp.Place = v?.ToString();

            v = GetValueByNames("PANNum", "PAN", "Pan");
            emp.PANNum = v?.ToString();

            v = GetValueByNames("Aadhar", "AadharNumber", "Aadhaar");
            emp.Aadhar = v?.ToString();

            v = GetValueByNames("Mobile_Num", "MobileNum", "Mobile", "Phone");
            emp.MobileNum = v?.ToString();

            v = GetValueByNames("Department", "Dept");
            emp.Department = v?.ToString();

            v = GetValueByNames("Specialization", "Speciality");
            emp.Specialization = v?.ToString();

            v = GetValueByNames("Availability_Status", "AvailabilityStatus", "Availability");
            emp.AvailabilityStatus = v?.ToString();

            v = GetValueByNames("Emp_Status", "EmpStatus", "Status");
            emp.EmpStatus = v?.ToString();

            v = GetValueByNames("Joining_Date", "JoiningDate", "Joining_Date");
            if (v != null && DateTime.TryParse(v.ToString(), out var jd)) emp.JoiningDate = jd;

            v = GetValueByNames("Created_Date", "CreatedDate", "Created_On");
            if (v != null && DateTime.TryParse(v.ToString(), out var cd)) emp.CreatedDate = cd;

            return emp;
        }

        public async Task UpdateEmployeeAsync(Employee employee, CancellationToken cancellationToken = default)
        {
            string? procName = null;
            try
            {
                using var conn = _context.Database.GetDbConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "usp_EMP_UpdateById";
                procName = cmd.CommandText;
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@EmpId", employee.EmpId));
                cmd.Parameters.Add(new SqlParameter("@EmpName", employee.EmpName ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Department", employee.Department ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@Specialization", employee.Specialization ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@EmpStatus", employee.EmpStatus ?? (object)DBNull.Value));
                cmd.Parameters.Add(new SqlParameter("@JoiningDate", employee.JoiningDate));

                if (conn.State != ConnectionState.Open) await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                try
                {
                    if (!string.IsNullOrEmpty(procName) && !ex.Data.Contains("Procedure"))
                    {
                        ex.Data["Procedure"] = procName;
                    }
                }
                catch { }

                _logger?.LogError(ex, "Database error while updating employee with id {EmpId}. Proc:{Proc}", employee.EmpId, procName);

                throw new ApplicationException($"Database error while updating employee with id {employee.EmpId}.", ex);
            }
        }

        public async Task DeleteEmployeeAsync(int empId, CancellationToken cancellationToken = default)
        {
            string? procName = null;
            try
            {
                using var conn = _context.Database.GetDbConnection();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "usp_EMP_DeleteById";
                procName = cmd.CommandText;
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@EmpId", empId));

                if (conn.State != ConnectionState.Open) await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                try
                {
                    if (!string.IsNullOrEmpty(procName) && !ex.Data.Contains("Procedure"))
                    {
                        ex.Data["Procedure"] = procName;
                    }
                }
                catch { }

                _logger?.LogError(ex, "Database error while deleting employee with id {EmpId}. Proc:{Proc}", empId, procName);
                throw new ApplicationException($"Database error while deleting employee with id {empId}.", ex);
            }
        }
    }
}
