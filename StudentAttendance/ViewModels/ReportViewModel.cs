using System.Collections.Generic;

namespace StudentAttendance.ViewModels
{
    public class ReportViewModel
    {
        public string SelectedCourse { get; set; } = "IS Design";
        public string SelectedPeriod { get; set; } = "Week 6";

        public int AttendanceRate { get; set; }
        public int OnTimeRate { get; set; }
        public int LateRate { get; set; }
        public int AbsentRate { get; set; }

        public List<StudentReportRow> Students { get; set; } = new List<StudentReportRow>();
    }

    public class StudentReportRow
    {
        public string StudentName { get; set; } = string.Empty;
        public int PresentCount { get; set; }
        public int LateTier1Count { get; set; }
        public int LateTier2Count { get; set; }
        public int LateTier3Count { get; set; }
        public int AbsentCount { get; set; }
        public string RatePercentage { get; set; } = string.Empty;
    }
}
