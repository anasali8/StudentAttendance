namespace StudentAttendance.Core.Models;

public class Student
{
    public int Id { get; set; }

    /// <summary>
    /// The human-readable student identifier (e.g., "STU-2025-0412").
    /// This maps directly to the ZKTeco device's EnrollNumber string field,
    /// allowing the fingerprint scan event to look up the correct Student record.
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public int EnrollmentYear { get; set; }

    // Navigation Properties
    public Fingerprint? Fingerprint { get; set; }
    public ICollection<Course> Courses { get; set; } = new List<Course>();
}
