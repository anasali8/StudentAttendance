using StudentAttendance.ViewModels;

namespace StudentAttendance.Core.Interfaces;

/// <summary>
/// Defines the contract for student fingerprint enrollment.
/// Both AdminController and StudentEnrollmentController use this service,
/// ensuring consistent enrollment logic regardless of entry point.
/// </summary>
public interface IEnrollmentService
{
    /// <summary>
    /// Validates and persists a new Student record along with their encrypted Fingerprint template.
    /// </summary>
    /// <param name="model">The enrollment form data submitted by the user.</param>
    /// <returns>
    /// A tuple of (success: bool, errorMessage: string?).
    /// errorMessage is null on success, or a user-facing message on failure.
    /// </returns>
    Task<(bool success, string? errorMessage)> EnrollStudentAsync(EnrollmentViewModel model);
}
