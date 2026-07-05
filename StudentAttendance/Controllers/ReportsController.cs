using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentAttendance.Core.Enums;
using StudentAttendance.Infrastructure.Data;
using StudentAttendance.ViewModels;

namespace StudentAttendance.Controllers;

public class ReportsController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public ReportsController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Generates an attendance report for a given course and number of past weeks.
    /// Defaults to showing all courses across the last 6 weeks if no filters are provided.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> Index(int? courseId, int weekOffset = 6)
    {
        // 1. Determine the reporting date window
        var periodStart = DateTime.UtcNow.AddDays(-(weekOffset * 7));

        // 2. Build the attendance query — optionally filtered by course
        var attendanceQuery = _dbContext.Attendances
            .AsNoTracking()
            .Include(a => a.LectureSession)
            .Include(a => a.Student)
            .Where(a => a.LectureSession.ScheduledStart >= periodStart);

        if (courseId.HasValue)
        {
            attendanceQuery = attendanceQuery
                .Where(a => a.LectureSession.CourseId == courseId.Value);
        }

        var attendances = await attendanceQuery.ToListAsync();

        // 3. Compute aggregate KPI rates
        int totalRecords = attendances.Count;

        int onTimeCount  = attendances.Count(a => a.LatenessClassification == LatenessClassification.OnTime);
        int lateCount    = attendances.Count(a => a.LatenessClassification == LatenessClassification.LateTier1 ||
                                                   a.LatenessClassification == LatenessClassification.LateTier2 ||
                                                   a.LatenessClassification == LatenessClassification.LateTier3);
        int absentCount  = attendances.Count(a => a.LatenessClassification == LatenessClassification.Absent);

        // AttendanceRate = all non-absent records as a percentage of the total expected records
        int attendanceRate = totalRecords > 0 ? ((onTimeCount + lateCount) * 100 / totalRecords) : 0;
        int onTimeRate     = totalRecords > 0 ? (onTimeCount  * 100 / totalRecords) : 0;
        int lateRate       = totalRecords > 0 ? (lateCount    * 100 / totalRecords) : 0;
        int absentRate     = totalRecords > 0 ? (absentCount  * 100 / totalRecords) : 0;

        // 4. Build per-student rows, grouped by StudentId
        var studentRows = attendances
            .GroupBy(a => new { a.StudentId, a.Student.FullName })
            .Select(g =>
            {
                int totalSessions = g.Count();
                int present   = g.Count(a => a.LatenessClassification == LatenessClassification.OnTime);
                int tier1     = g.Count(a => a.LatenessClassification == LatenessClassification.LateTier1);
                int tier2     = g.Count(a => a.LatenessClassification == LatenessClassification.LateTier2);
                int tier3     = g.Count(a => a.LatenessClassification == LatenessClassification.LateTier3);
                int absent    = g.Count(a => a.LatenessClassification == LatenessClassification.Absent);
                int rateInt   = totalSessions > 0 ? ((present + tier1 + tier2 + tier3) * 100 / totalSessions) : 0;

                return new StudentReportRow
                {
                    StudentName      = g.Key.FullName,
                    PresentCount     = present,
                    LateTier1Count   = tier1,
                    LateTier2Count   = tier2,
                    LateTier3Count   = tier3,
                    AbsentCount      = absent,
                    RatePercentage   = $"{rateInt}%"
                };
            })
            .OrderBy(r => r.StudentName)
            .ToList();

        // 5. Resolve course name for filter display
        string selectedCourseName = "All Courses";
        if (courseId.HasValue)
        {
            var course = await _dbContext.Courses
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == courseId.Value);
            selectedCourseName = course?.CourseName ?? "All Courses";
        }

        var model = new ReportViewModel
        {
            SelectedCourse  = selectedCourseName,
            SelectedPeriod  = $"Last {weekOffset} Weeks",
            AttendanceRate  = attendanceRate,
            OnTimeRate      = onTimeRate,
            LateRate        = lateRate,
            AbsentRate      = absentRate,
            Students        = studentRows
        };

        return View(model);
    }
}
