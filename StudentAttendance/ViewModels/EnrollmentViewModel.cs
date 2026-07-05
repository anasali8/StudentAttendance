using System.ComponentModel.DataAnnotations;

namespace StudentAttendance.ViewModels;

public class EnrollmentViewModel
{
    [Required]
    [Display(Name = "Student ID")]
    public string StudentIdString { get; set; } = string.Empty; // e.g., STU-2025-0412

    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required]
    public string Department { get; set; } = string.Empty;

    [Required]
    public string Year { get; set; } = string.Empty;

    // This will be populated by the client-side hardware listener or hidden input
    public string FingerprintBase64 { get; set; } = string.Empty; 
}
