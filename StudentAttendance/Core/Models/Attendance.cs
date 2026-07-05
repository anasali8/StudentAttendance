using StudentAttendance.Core.Enums;

namespace StudentAttendance.Core.Models;

public class Attendance
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int LectureSessionId { get; set; }
    public LectureSession LectureSession { get; set; } = null!;

    public DateTime? Timestamp { get; set; } // Nullable if Absent
    public LatenessClassification LatenessClassification { get; set; }
    public bool IsManualOverride { get; set; }
}
