using Microsoft.EntityFrameworkCore;
using HospIntel.EmployeeService.Models;

namespace HospIntel.EmployeeService.Data
{
    public class EmployeeDbContext : DbContext
    {
        public EmployeeDbContext(DbContextOptions<EmployeeDbContext> options)
            : base(options)
        {
        }

        public DbSet<Employee> Employees { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Ensure EF Core recognizes the primary key for Employee
            modelBuilder.Entity<Employee>().HasKey(e => e.EmpId);

            base.OnModelCreating(modelBuilder);
        }
    }
}
