namespace StudentAttendance.Core.Models;

public class Course
{
    public int Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;

    public int TeacherId { get; set; }
    public User Teacher { get; set; } = null!;

    // Navigation Properties
    public ICollection<Student> Students { get; set; } = new List<Student>();
    public ICollection<LectureSession> LectureSessions { get; set; } = new List<LectureSession>();
}
