using StudentAttendance.Core.Enums;

namespace StudentAttendance.Core.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }

    /// <summary>
    /// Links a User account to the associated Student or Teacher record ID (e.g., "T-402", "S-202301").
    /// Blank for System Administrators who have no associated Student/Teacher record.
    /// </summary>
    public string AssociatedId { get; set; } = string.Empty;

    /// <summary>
    /// Controls whether this user can log into the system.
    /// Soft-delete pattern: inactive users are retained for audit history.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
