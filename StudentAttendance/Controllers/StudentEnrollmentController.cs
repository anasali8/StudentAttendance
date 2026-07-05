using Microsoft.AspNetCore.Mvc;
using StudentAttendance.Core.Interfaces;
using StudentAttendance.ViewModels;
using StudentAttendance.Infrastructure.Data;

namespace StudentAttendance.Controllers;

public class StudentEnrollmentController : Controller
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly ApplicationDbContext _dbContext;

    public StudentEnrollmentController(IEnrollmentService enrollmentService, ApplicationDbContext dbContext)
    {
        _enrollmentService = enrollmentService;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? studentId)
    {
        var model = new EnrollmentViewModel();
        
        if (studentId.HasValue)
        {
            var student = await _dbContext.Students.FindAsync(studentId.Value);
            if (student != null)
            {
                model.StudentIdString = student.ExternalId;
                model.FullName = student.FullName;
                model.Department = student.Department;
                model.Year = student.EnrollmentYear.ToString();
            }
        }
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(EnrollmentViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var (success, errorMessage) = await _enrollmentService.EnrollStudentAsync(model);

        if (!success)
        {
            ModelState.AddModelError(string.Empty, errorMessage ?? "Enrollment failed.");
            return View(model);
        }

        TempData["SuccessMessage"] = $"Student '{model.FullName}' enrolled successfully!";
        return RedirectToAction(nameof(Index));
    }
}
