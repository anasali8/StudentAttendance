using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using StudentAttendance.Core.Interfaces;
using StudentAttendance.Core.Models;
using StudentAttendance.Infrastructure.Data;
using StudentAttendance.Hubs;
using Microsoft.Extensions.Logging;

namespace StudentAttendance.Core.Services;

public class AttendanceService : IAttendanceService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILatenessEngine _latenessEngine;
    private readonly IHubContext<AttendanceHub, IAttendanceHubClient> _hubContext;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        ApplicationDbContext dbContext,
        ILatenessEngine latenessEngine,
        IHubContext<AttendanceHub, IAttendanceHubClient> hubContext,
        ILogger<AttendanceService> logger)
    {
        _dbContext = dbContext;
        _latenessEngine = latenessEngine;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task ProcessCheckInAsync(string enrollNumber, DateTime timestamp)
    {
        // 1. Fetch the Student by their ExternalId (ZKTeco EnrollNumber)
        //    ExternalId is the string stored on the device that uniquely identifies a student.
        var student = await _dbContext.Students
            .Include(s => s.Courses)
            .FirstOrDefaultAsync(s => s.ExternalId == enrollNumber);

        if (student == null)
        {
            _logger.LogWarning("Check-in failed: No student found with ExternalId '{EnrollNumber}'.", enrollNumber);
            await _hubContext.Clients.All.ReceiveScanError($"Student with ID {enrollNumber} not found.");
            return;
        }

        // 2. Find the currently active LectureSession for any of the student's courses.
        //    A class is active if the current time falls within its scheduled window
        //    (with a 30-minute early buffer to allow students to arrive ahead of time).
        var activeSession = await _dbContext.LectureSessions
            .Include(ls => ls.Course)
            .Where(ls => ls.IsActive && student.Courses.Select(c => c.Id).Contains(ls.CourseId))
            .Where(ls => timestamp >= ls.ScheduledStart.AddMinutes(-30) && timestamp <= ls.ScheduledEnd)
            .FirstOrDefaultAsync();

        if (activeSession == null)
        {
            _logger.LogWarning("Check-in failed: No active lecture session found for ExternalId '{EnrollNumber}' at {Timestamp}.", enrollNumber, timestamp);
            await _hubContext.Clients.All.ReceiveScanError($"No active session for {student.FullName}.");
            return;
        }

        // 3. Calculate the lateness tier based on how late the student arrived
        var classification = _latenessEngine.CalculateLateness(activeSession.ScheduledStart, timestamp);

        // 4. Guard against duplicate check-ins for the same session.
        //    The DB also has a unique index on (LectureSessionId, StudentId) as a safety net.
        var existingAttendance = await _dbContext.Attendances
            .FirstOrDefaultAsync(a => a.StudentId == student.Id && a.LectureSessionId == activeSession.Id);

        if (existingAttendance != null)
        {
            _logger.LogInformation("Duplicate check-in ignored for Student '{FullName}' (ExternalId={EnrollNumber}), Session {SessionId}.",
                student.FullName, enrollNumber, activeSession.Id);
            return;
        }

        // 5. Persist the attendance record
        var attendanceRecord = new Attendance
        {
            StudentId              = student.Id,
            LectureSessionId       = activeSession.Id,
            Timestamp              = timestamp,
            LatenessClassification = classification,
            IsManualOverride       = false
        };

        _dbContext.Attendances.Add(attendanceRecord);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // This can occur in a race condition where two scan events arrive simultaneously
            // for the same student+session. The DB unique index will reject the second write.
            _logger.LogWarning(ex, "Duplicate attendance record race condition detected for Student {StudentId}, Session {SessionId}. Second write discarded.", student.Id, activeSession.Id);
            return;
        }

        _logger.LogInformation("Check-in recorded: Student='{FullName}', Course='{CourseCode}', Status={Classification}.",
            student.FullName, activeSession.Course.CourseCode, classification);

        // 6. Push the real-time update to all connected Teacher Dashboard clients via SignalR
        await _hubContext.Clients.All.ReceiveAttendanceUpdate(
            student.Id,
            student.FullName,
            classification.ToString(),
            timestamp.ToString("hh:mm tt")
        );
    }
}
