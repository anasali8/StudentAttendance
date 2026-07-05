using System.Collections.Generic;

namespace StudentAttendance.ViewModels
{
    public class AdminManagementViewModel
    {
        public List<UserDto> Users { get; set; } = new List<UserDto>();
        
        // Fields for creating/modifying a user
        public string NewUsername { get; set; } = string.Empty;
        public string NewUserRole { get; set; } = "Student";
        public string AssociatedId { get; set; } = string.Empty; // Student/Teacher ID
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string AssociatedId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
