using StudentAttendance.Core.Enums;

namespace StudentAttendance.Core.Models;

public class Prediction
{
    public int Id { get; set; }

    public int StudentId { get; set; }
    public Student Student { get; set; } = null!;

    public int CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public DateTime NextLectureDate { get; set; }
    public decimal AbsenceProbability { get; set; }
    public RiskFlag RiskFlag { get; set; }
}
