using Microsoft.AspNetCore.Mvc;
using StudentAttendance.Core.Interfaces;

namespace StudentAttendance.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FingerprintController : ControllerBase
{
    private readonly IZKTecoService _zkTecoService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FingerprintController> _logger;

    public FingerprintController(
        IZKTecoService zkTecoService, 
        IConfiguration configuration,
        ILogger<FingerprintController> logger)
    {
        _zkTecoService = zkTecoService;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("sync/{studentId}")]
    public IActionResult SyncFingerprint(string studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
            return BadRequest(new { success = false, message = "Student ID is required." });

        try
        {
            // Ensure connection (harmless if already connected via Singleton)
            string ipAddress = _configuration["ZKTeco:IpAddress"] ?? "192.168.1.201";
            int port = int.TryParse(_configuration["ZKTeco:Port"], out int p) ? p : 4370;
            bool isConnected = _zkTecoService.Connect(ipAddress, port);
            
            if (!isConnected)
            {
                return Ok(new { success = false, message = "Could not connect to ZKTeco device over network." });
            }

            byte[] templateBytes = _zkTecoService.GetUserTemplate(studentId);
            
            if (templateBytes == null || templateBytes.Length == 0)
            {
                return Ok(new { success = false, message = $"No fingerprint found on device for ID: {studentId}." });
            }

            string base64Template = Convert.ToBase64String(templateBytes);
            
            return Ok(new { success = true, templateBase64 = base64Template });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pulling template for {StudentId}", studentId);
            return Ok(new { success = false, message = "An error occurred while communicating with the device." });
        }
    }

    [HttpGet("mock")]
    public IActionResult MockFingerprint()
    {
        // Must be a valid Base64 string so Convert.FromBase64String doesn't throw a FormatException
        return Ok(new { success = true, templateBase64 = "TW9ja0ZpbmdlcnByaW50RGF0YQ==" });
    }
}
