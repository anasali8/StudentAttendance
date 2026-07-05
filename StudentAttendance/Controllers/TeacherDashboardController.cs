using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using StudentAttendance.Infrastructure.Data;
using StudentAttendance.ViewModels;
using System;

namespace StudentAttendance.Controllers;

public class TeacherDashboardController : Controller
{
    private readonly ApplicationDbContext _dbContext;

    public TeacherDashboardController(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index(int teacherId = 1) // Defaulting to 1 for demonstration
    {
        // 1. Find an active lecture session for this teacher
        var activeSession = await _dbContext.LectureSessions
            .Include(ls => ls.Course)
                .ThenInclude(c => c.Students)
            .FirstOrDefaultAsync(ls => ls.IsActive && ls.Course.TeacherId == teacherId);

        if (activeSession == null)
        {
            return View("NoActiveSession");
        }

        // 2. Gather today's attendance records for this session
        var attendances = await _dbContext.Attendances
            .Where(a => a.LectureSessionId == activeSession.Id)
            .ToListAsync();

        // 3. Gather AI Predictions for the students in this course
        var studentIds = activeSession.Course.Students.Select(s => s.Id).ToList();
        var predictions = await _dbContext.Predictions
            .Where(p => p.CourseId == activeSession.CourseId && studentIds.Contains(p.StudentId))
            .ToDictionaryAsync(p => p.StudentId);

        // 4. Map to ViewModel
        var viewModel = new DashboardViewModel
        {
            ActiveLectureSessionId = activeSession.Id,
            CourseName = activeSession.Course.CourseName,
            CourseCode = activeSession.Course.CourseCode,
            TotalEnrolled = activeSession.Course.Students.Count,
            TotalPresent = attendances.Count(a => a.LatenessClassification == Core.Enums.LatenessClassification.OnTime),
            TotalLate = attendances.Count(a => a.LatenessClassification != Core.Enums.LatenessClassification.OnTime && 
                                               a.LatenessClassification != Core.Enums.LatenessClassification.Absent)
        };

        foreach (var student in activeSession.Course.Students)
        {
            var attendanceRecord = attendances.FirstOrDefault(a => a.StudentId == student.Id);
            var predictionRecord = predictions.ContainsKey(student.Id) ? predictions[student.Id] : null;

            viewModel.Students.Add(new StudentAttendanceInfo
            {
                StudentId = student.Id,
                FullName = student.FullName,
                Status = attendanceRecord != null ? attendanceRecord.LatenessClassification.ToString() : "Pending",
                CheckInTime = attendanceRecord != null && attendanceRecord.Timestamp.HasValue 
                              ? attendanceRecord.Timestamp.Value.ToString("hh:mm tt") 
                              : "--:--",
                AbsenceProbability = predictionRecord?.AbsenceProbability ?? 0,
                RiskFlag = predictionRecord?.RiskFlag ?? Core.Enums.RiskFlag.Low
            });
        }

        return View(viewModel);
    }
}
