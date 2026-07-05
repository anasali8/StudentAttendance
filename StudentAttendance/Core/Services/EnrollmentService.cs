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

    public EnrollmentService(
        ApplicationDbContext dbContext,
        IEncryptionService encryptionService,
        ILogger<EnrollmentService> logger)
    {
        _dbContext = dbContext;
        _encryptionService = encryptionService;
        _logger = logger;
    }

    public async Task<(bool success, string? errorMessage)> EnrollStudentAsync(EnrollmentViewModel model)
    {
        // 1. Guard: ensure ExternalId is not already in use
        bool alreadyExists = await _dbContext.Students
            .AnyAsync(s => s.ExternalId == model.StudentIdString);

        if (alreadyExists)
        {
            _logger.LogWarning("Enrollment rejected: ExternalId '{ExternalId}' is already registered.", model.StudentIdString);
            return (false, $"A student with ID '{model.StudentIdString}' is already enrolled in the system.");
        }

        // 2. Parse the enrollment year — EnrollmentViewModel stores it as a string (e.g., "3rd Year")
        // We extract the leading integer if present, otherwise store the current calendar year.
        int enrollmentYear = DateTime.UtcNow.Year;
        var yearDigits = new string(model.Year.Where(char.IsDigit).ToArray());
        if (yearDigits.Length > 0 && int.TryParse(yearDigits, out int parsedYear) && parsedYear >= 1 && parsedYear <= 10)
        {
            // It's an academic year indicator (e.g., "3" from "3rd Year"), not a calendar year.
            // Store the current calendar year as enrollment year for the DB record.
            enrollmentYear = DateTime.UtcNow.Year;
        }

        // 3. Create the Student entity
        var student = new Student
        {
            ExternalId     = model.StudentIdString,
            FullName       = model.FullName,
            Department     = model.Department,
            EnrollmentYear = enrollmentYear,
            Email          = string.Empty // Email not collected in the enrollment form currently
        };

        // 4. If a fingerprint template was captured from the hardware, encrypt and attach it
        if (!string.IsNullOrWhiteSpace(model.FingerprintBase64))
        {
            try
            {
                byte[] rawTemplate = Convert.FromBase64String(model.FingerprintBase64);
                byte[] encryptedTemplate = _encryptionService.Encrypt(rawTemplate);

                student.Fingerprint = new Fingerprint
                {
                    EncryptedTemplate = encryptedTemplate,
                    EnrollmentDate    = DateTime.UtcNow
                };
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "Invalid Base64 fingerprint template submitted for ExternalId '{ExternalId}'.", model.StudentIdString);
                return (false, "The fingerprint data received from the scanner is invalid. Please try scanning again.");
            }
        }

        _dbContext.Students.Add(student);

        try
        {
            await _dbContext.SaveChangesAsync();
            _logger.LogInformation("Student enrolled successfully: ExternalId='{ExternalId}', Name='{FullName}'.", student.ExternalId, student.FullName);
            return (true, null);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error while enrolling student ExternalId='{ExternalId}'.", model.StudentIdString);
            return (false, "A database error occurred during enrollment. Please try again.");
        }
    }
}
