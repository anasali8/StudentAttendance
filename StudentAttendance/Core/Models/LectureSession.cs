namespace StudentAttendance.Core.Models;

public class LectureSession
{
    public int Id { get; set; }
    
    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public bool IsActive { get; set; }

    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}
