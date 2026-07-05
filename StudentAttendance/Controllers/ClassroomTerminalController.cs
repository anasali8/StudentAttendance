using Microsoft.AspNetCore.Mvc;
using StudentAttendance.Core.Interfaces;
using System.Threading.Tasks;
using System;

namespace StudentAttendance.Controllers;

public class ClassroomTerminalController : Controller
{
    private readonly IAttendanceService _attendanceService;

    public ClassroomTerminalController(IAttendanceService attendanceService)
    {
        _attendanceService = attendanceService;
    }

    public IActionResult Index()
    {
        // Displays the kiosk interface waiting for fingerprint scans
        return View();
    }

    // This endpoint acts as a manual fallback if the hardware scanner is broken 
    // and the teacher manually types in a student's External ID from the terminal.
    [HttpPost]
    public async Task<IActionResult> ManualCheckIn(string studentId)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            return BadRequest(new { success = false, error = "Student ID cannot be empty." });
        }

        await _attendanceService.ProcessCheckInAsync(studentId, DateTime.UtcNow);
        return Ok(new { success = true });
    }
}
