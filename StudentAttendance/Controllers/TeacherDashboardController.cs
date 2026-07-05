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

    public async Task<IActionResult> Index(int teacherId = 2) // Defaulting to 2 (Dr. Smith) for demonstration
    {
        // 1. Find an active lecture session for this teacher
        var activeSession = await _dbContext.LectureSessions
            .Include(ls => ls.Course)
                .ThenInclude(c => c.Students)
            .FirstOrDefaultAsync(ls => ls.IsActive && ls.Course.TeacherId == teacherId);

        if (activeSession == null)
        {
            var courses = await _dbContext.Courses
                .Where(c => c.TeacherId == teacherId)
                .Select(c => new CourseItemViewModel
                {
                    CourseId = c.Id,
                    CourseCode = c.CourseCode,
                    CourseName = c.CourseName
                })
                .ToListAsync();

            var noSessionModel = new NoActiveSessionViewModel
            {
                TeacherId = teacherId,
                TeacherCourses = courses
            };

            return View("NoActiveSession", noSessionModel);
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StartSession(int teacherId, int courseId)
    {
        // Close any existing active session for this teacher
        var existingActive = await _dbContext.LectureSessions
            .Include(ls => ls.Course)
            .Where(ls => ls.IsActive && ls.Course.TeacherId == teacherId)
            .ToListAsync();

        foreach (var session in existingActive)
        {
            session.IsActive = false;
            session.ScheduledEnd = DateTime.Now;
        }

        // Create a new active session
        var newSession = new StudentAttendance.Core.Models.LectureSession
        {
            CourseId = courseId,
            ScheduledStart = DateTime.Now,
            ScheduledEnd = DateTime.Now.AddHours(2), // Default 2 hours duration
            IsActive = true
        };

        _dbContext.LectureSessions.Add(newSession);
        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Lecture session started successfully!";
        return RedirectToAction(nameof(Index), new { teacherId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EndSession(int sessionId, int teacherId)
    {
        var session = await _dbContext.LectureSessions
            .Include(ls => ls.Course)
                .ThenInclude(c => c.Students)
            .FirstOrDefaultAsync(ls => ls.Id == sessionId);

        if (session != null)
        {
            session.IsActive = false;
            session.ScheduledEnd = DateTime.Now;

            // Auto-mark pending students as absent
            var existingAttendances = await _dbContext.Attendances
                .Where(a => a.LectureSessionId == sessionId)
                .Select(a => a.StudentId)
                .ToListAsync();

            foreach (var student in session.Course.Students)
            {
                if (!existingAttendances.Contains(student.Id))
                {
                    _dbContext.Attendances.Add(new StudentAttendance.Core.Models.Attendance
                    {
                        StudentId = student.Id,
                        LectureSessionId = sessionId,
                        Timestamp = null,
                        LatenessClassification = Core.Enums.LatenessClassification.Absent,
                        IsManualOverride = false
                    });
                }
            }

            await _dbContext.SaveChangesAsync();
            TempData["SuccessMessage"] = "Session ended successfully. Missing students were marked as absent.";
        }

        return RedirectToAction(nameof(Index), new { teacherId });
    }
}
