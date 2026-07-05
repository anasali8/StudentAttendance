using System.Collections.Generic;

namespace StudentAttendance.ViewModels;

public class NoActiveSessionViewModel
{
    public int TeacherId { get; set; }
    public List<CourseItemViewModel> TeacherCourses { get; set; } = new List<CourseItemViewModel>();
}

public class CourseItemViewModel
{
    public int CourseId { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
}
