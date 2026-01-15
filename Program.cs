using Microsoft.EntityFrameworkCore;      // USed for Entity Framework Core
using HospIntel.EmployeeService.Data;     // it will enable the Data folder
using HospIntel.EmployeeService.Services; // it will enable the Services folder

var builder = WebApplication.CreateBuilder(args); //it will call appsettings.json and other configuration files

// Add controllers and services
builder.Services.AddControllers(); // it will add controller services

// SQL Server DbContext with pooling and resilient SQL retry
builder.Services.AddDbContextPool<EmployeeDbContext>(options =>  // DbContext pooling for performance
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(5, TimeSpan.FromSeconds(2), null) // Resilient SQL retry
    )
);

// Dependency Injection
// Use scoped service for business logic; controller methods are async for throughput
builder.Services.AddScoped<IEmployeeService, EmployeeService>(); // Employee service DI

// Swagger
builder.Services.AddEndpointsApiExplorer();// it will add endpoint api explorer
builder.Services.AddSwaggerGen();// it will add swagger generator

var app = builder.Build();// it will build the app

// Serve Swagger UI only in Development
if (app.Environment.IsDevelopment())
{
    // Serve Swagger UI at application root ("/") so IIS Express opens the Swagger page automatically in Development
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Employee Service API v1"); // Swagger endpoint
        c.RoutePrefix = string.Empty; // serve at '/' 
    });
}

app.UseHttpsRedirection();// it will redirect http to https

// Request/response logging middleware - logs request and response payloads and timing
app.UseMiddleware<HospIntel.EmployeeService.Middleware.RequestResponseLoggingMiddleware>();// it will use custom middleware for logging

app.UseAuthorization();// it will use authorization middleware

app.MapControllers();//it will map controller routes

app.Run();// it will run the app

ThreadPool.SetMinThreads(workerThreads: 200, completionPortThreads: 200);
