using System;
using System.Collections.Generic;
using StudentAttendance.Core.Enums;

namespace StudentAttendance.ViewModels;

public class DashboardViewModel
{
    public int ActiveLectureSessionId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    
    // Live attendance statistics
    public int TotalEnrolled { get; set; }
    public int TotalPresent { get; set; }
    public int TotalLate { get; set; }
    
    // Students and their current status for today
    public List<StudentAttendanceInfo> Students { get; set; } = new List<StudentAttendanceInfo>();
}

public class StudentAttendanceInfo
{
    public int StudentId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string CheckInTime { get; set; } = "--:--";
    
    // ML.NET AI Prediction Data
    public decimal AbsenceProbability { get; set; }
    public RiskFlag RiskFlag { get; set; }
}
