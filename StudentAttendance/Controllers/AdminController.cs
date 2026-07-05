using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentAttendance.Core.Interfaces;
using StudentAttendance.Infrastructure.Data;
using StudentAttendance.ViewModels;

namespace StudentAttendance.Controllers;

public class AdminController : Controller
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEnrollmentService _enrollmentService;

    public AdminController(ApplicationDbContext dbContext, IEnrollmentService enrollmentService)
    {
        _dbContext = dbContext;
        _enrollmentService = enrollmentService;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .OrderBy(u => u.Role)
            .ThenBy(u => u.Username)
            .ToListAsync();

        var model = new AdminManagementViewModel
        {
            Users = users.Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role.ToString(),
                AssociatedId = string.IsNullOrEmpty(u.AssociatedId) ? "N/A" : u.AssociatedId,
                IsActive = u.IsActive
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Enrollment()
    {
        return View(new EnrollmentViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enrollment(EnrollmentViewModel model)
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

        TempData["SuccessMessage"] = $"Successfully enrolled student '{model.FullName}' with fingerprint.";
        return RedirectToAction(nameof(Enrollment));
    }
}
