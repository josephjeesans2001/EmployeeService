using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HospIntel.EmployeeService.Models
{
    public class Employee
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EmpId { get; set; }

        // From EMP_Master: EMP_Name
        public string? EmpName { get; set; }

        // Date_of_Birth
        public DateTime? DateOfBirth { get; set; }

        // Place
        public string? Place { get; set; }

        // PANNum
        public string? PANNum { get; set; }

        // Aadhar
        public string? Aadhar { get; set; }

        // Mobile_Num
        public string? MobileNum { get; set; }

        public string? Department { get; set; }
        public string? Specialization { get; set; }

        // Availability_Status
        public string? AvailabilityStatus { get; set; }

        // Emp_Status
        public string? EmpStatus { get; set; }

        // Joining_Date
        public DateTime? JoiningDate { get; set; }

        // Created_Date
        public DateTime? CreatedDate { get; set; }
    }
}
