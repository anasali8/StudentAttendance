using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentAttendance.Core.Interfaces;
using StudentAttendance.Core.Models;
using StudentAttendance.Infrastructure.Data;
using StudentAttendance.ViewModels;

namespace StudentAttendance.Core.Services;

/// <summary>
/// Concrete implementation of IEnrollmentService.
/// Handles the full enrollment workflow: duplicate check → Student insert → Fingerprint insert.
/// Registered as Scoped in Program.cs to match the ApplicationDbContext lifetime.
/// </summary>
public class EnrollmentService : IEnrollmentService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<EnrollmentService> _logger;
    private readonly IAttendanceService _attendanceService;

    public EnrollmentService(
        ApplicationDbContext dbContext,
        IEncryptionService encryptionService,
        ILogger<EnrollmentService> logger,
        IAttendanceService attendanceService)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _logger = logger;
        _attendanceService = attendanceService;
    }

    public async Task<(bool success, string? errorMessage)> EnrollStudentAsync(EnrollmentViewModel model)
    {
        // 1. Check if the student already exists in the system
        var existingStudent = await _dbContext.Students
            .Include(s => s.Fingerprint)
            .FirstOrDefaultAsync(s => s.ExternalId == model.StudentIdString);

        Student studentToSave;

        if (existingStudent != null)
        {
            // We are attaching a fingerprint to an existing student
            studentToSave = existingStudent;
            // Optionally update basic info if the form had changes
            studentToSave.FullName = model.FullName;
            studentToSave.Department = model.Department;
        }
        else
        {
            // 2. We are creating a brand new student from scratch
            int enrollmentYear = DateTime.UtcNow.Year;
            var yearDigits = new string(model.Year.Where(char.IsDigit).ToArray());
            if (yearDigits.Length > 0 && int.TryParse(yearDigits, out int parsedYear) && parsedYear >= 1 && parsedYear <= 10)
            {
                enrollmentYear = DateTime.UtcNow.Year;
            }

            studentToSave = new Student
            {
                ExternalId     = model.StudentIdString,
                FullName       = model.FullName,
                Department     = model.Department,
                EnrollmentYear = enrollmentYear,
                Email          = string.Empty, // Email not collected in the enrollment form currently
                Courses        = await _dbContext.Courses.ToListAsync()
            };

            _dbContext.Students.Add(studentToSave);
        }

        // 3. If a fingerprint template was captured from the hardware, encrypt and attach it
        if (!string.IsNullOrWhiteSpace(model.FingerprintBase64))
        {
            try
            {
                // Sanitize string to remove any accidental whitespace, newlines, or invalid characters
                string sanitizedB64 = model.FingerprintBase64.Trim().Replace(" ", "+").Replace("\r", "").Replace("\n", "");
                
                // If it's still the old hardcoded mock string from browser cache, manually override it to avoid crash
                if (sanitizedB64 == "MOCKED_BASE64_FINGERPRINT_DATA_==") 
                {
                    sanitizedB64 = "TW9ja0ZpbmdlcnByaW50RGF0YQ==";
                }

                byte[] rawTemplate = Convert.FromBase64String(sanitizedB64);
                byte[] encryptedTemplate = _encryptionService.Encrypt(rawTemplate);

                if (studentToSave.Fingerprint != null)
                {
                    // Overwrite existing template if they are re-enrolling their finger
                    studentToSave.Fingerprint.EncryptedTemplate = encryptedTemplate;
                    studentToSave.Fingerprint.EnrollmentDate = DateTime.UtcNow;
                }
                else
                {
                    // Create new fingerprint
                    studentToSave.Fingerprint = new Fingerprint
                    {
                        EncryptedTemplate = encryptedTemplate,
                        EnrollmentDate    = DateTime.UtcNow
                    };
                }
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Invalid Base64 fingerprint template submitted for ExternalId '{ExternalId}'.", model.StudentIdString);
                return (false, "The fingerprint data received from the scanner is invalid. Please try scanning again.");
            }
        }

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Student enrolled successfully: ExternalId='{ExternalId}', Name='{FullName}'.", studentToSave.ExternalId, studentToSave.FullName);

            // Automatically check the student in if there is an active lecture session
            await _attendanceService.ProcessCheckInAsync(studentToSave.ExternalId, DateTime.Now);

            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while enrolling student ExternalId='{ExternalId}'.", model.StudentIdString);
            return (false, "A database error occurred during enrollment. Please try again.");
        }
    }
}
