using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StudentAttendance.Core.Interfaces;
using StudentAttendance.Infrastructure.Data;

namespace StudentAttendance.Infrastructure.Services;

public class ZKTecoDeviceListenerHostedService : BackgroundService
{
    private readonly ILogger<ZKTecoDeviceListenerHostedService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private IZKTecoService? _zkTecoService;

    public ZKTecoDeviceListenerHostedService(
        ILogger<ZKTecoDeviceListenerHostedService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ZKTeco Device Listener Background Service is starting.");

        // We create a scope just to resolve IZKTecoService, as it's likely registered as Scoped or Transient.
        // Alternatively, if IZKTecoService is Singleton, we could inject it directly into the constructor.
        using (var scope = _serviceProvider.CreateScope())
        {
            _zkTecoService = scope.ServiceProvider.GetRequiredService<IZKTecoService>();

            string ipAddress = _configuration["ZKTeco:IpAddress"] ?? "192.168.1.201";
            int port = int.TryParse(_configuration["ZKTeco:Port"], out int p) ? p : 4370;

            bool isConnected = _zkTecoService.Connect(ipAddress, port);

            if (isConnected)
            {
                _logger.LogInformation($"Successfully connected to ZKTeco device at {ipAddress}:{port}.");

                // Register the event callback for real-time attendances
                _zkTecoService.RegisterAttendanceEventListener(HandleAttendanceEvent);
            }
            else
            {
                _logger.LogError($"Failed to connect to ZKTeco device at {ipAddress}:{port}. Will continue running, but no events will be received.");
            }

            // Keep the background service alive as long as the application is running
            while (!stoppingToken.IsCancellationRequested)
            {
                // In a real production scenario, you might want to add connection health checks here
                // e.g., if (deviceDisconnected) { TryReconnect(); }
                
                await Task.Delay(10000, stoppingToken); // Yield control to prevent CPU starvation
            }

            _logger.LogInformation("ZKTeco Device Listener is stopping.");
            _zkTecoService.Disconnect();
        }
    }

    private void HandleAttendanceEvent(string enrollNumber, DateTime timestamp)
    {
        _logger.LogInformation($"Raw Fingerprint Event Received: EnrollNumber={enrollNumber} at {timestamp}");

        // CRITICAL: We are now in a background thread triggered by a COM event. 
        // We MUST create a new dependency injection scope to resolve Scoped services like ApplicationDbContext.
        // Using ApplicationDbContext outside a scope or across threads will cause a concurrency exception.
        
        // We use Task.Run so we don't block the COM event thread waiting for the DB
        Task.Run(async () => 
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var attendanceService = scope.ServiceProvider.GetRequiredService<IAttendanceService>();

                // Pass the enrollNumber string directly to the service.
                // AttendanceService will look it up against Student.ExternalId in the DB.
                await attendanceService.ProcessCheckInAsync(enrollNumber, timestamp);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing attendance event for {enrollNumber}");
            }
        });
    }
}
