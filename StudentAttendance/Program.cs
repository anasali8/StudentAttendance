using Microsoft.EntityFrameworkCore;
using StudentAttendance.Core.Interfaces;
using StudentAttendance.Core.Services;
using StudentAttendance.Infrastructure.Data;
using StudentAttendance.Infrastructure.Services;
using StudentAttendance.Hubs;

var builder = WebApplication.CreateBuilder(args);

// 1. Setup EF Core DbContext
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Add MVC Controllers and Views
builder.Services.AddControllersWithViews();

// 3. Add SignalR
builder.Services.AddSignalR();

// 4. Register Core Services & Engines
builder.Services.AddScoped<IEncryptionService, AesEncryptionService>();
builder.Services.AddScoped<ILatenessEngine, LatenessEngine>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddSingleton<IZKTecoService, ZKTecoService>();

// 5. Register Background Hosted Services (Workers)
// Hardware Listener (ZKTeco Device)
builder.Services.AddHostedService<ZKTecoDeviceListenerHostedService>();
// ML.NET AI Predictive Forecasting Engine
builder.Services.AddHostedService<PredictionHostedService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// 6. Map SignalR Hub
app.MapHub<AttendanceHub>("/attendanceHub");

// 7. Map Default Controller Route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=TeacherDashboard}/{action=Index}/{id?}");

app.Run();
