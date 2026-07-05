using System.ComponentModel.DataAnnotations;

namespace StudentAttendance.ViewModels;

public class StudentListViewModel
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int EnrollmentYear { get; set; }
    public bool HasFingerprintEnrolled { get; set; }
}

public class StudentFormViewModel
{
    public int Id { get; set; }

    [Display(Name = "Student ID")]
    public string? ExternalId { get; set; }

    [Required]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Required]
    public string Department { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Enrollment Year")]
    [Range(2000, 2100)]
    public int EnrollmentYear { get; set; } = DateTime.Now.Year;
}
