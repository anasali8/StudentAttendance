using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudentAttendance.Core.Models;
using StudentAttendance.Infrastructure.Data;
using StudentAttendance.ViewModels;

namespace StudentAttendance.Controllers;

public class StudentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public StudentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var students = await _context.Students
            .Include(s => s.Fingerprint)
            .AsNoTracking()
            .OrderBy(s => s.FullName)
            .ToListAsync();

        var model = students.Select(s => new StudentListViewModel
        {
            Id = s.Id,
            ExternalId = s.ExternalId,
            FullName = s.FullName,
            Department = s.Department,
            EnrollmentYear = s.EnrollmentYear,
            HasFingerprintEnrolled = s.Fingerprint != null
        }).ToList();

        return View(model);
    }

    private async Task PopulateAvailableCoursesAsync(StudentFormViewModel model)
    {
        model.AvailableCourses = await _context.Courses
            .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.CourseCode + " - " + c.CourseName
            })
            .ToListAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new StudentFormViewModel();
        await PopulateAvailableCoursesAsync(model);
        ViewData["Action"] = "Create";
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StudentFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateAvailableCoursesAsync(model);
            ViewData["Action"] = "Create";
            return View("Form", model);
        }

        // Auto-generate ExternalId if left blank
        if (string.IsNullOrWhiteSpace(model.ExternalId))
        {
            var random = new Random();
            string generatedId;
            bool isUnique;
            do
            {
                generatedId = $"STU-{model.EnrollmentYear}-{random.Next(1000, 9999)}";
                isUnique = !await _context.Students.AnyAsync(s => s.ExternalId == generatedId);
            } while (!isUnique);

            model.ExternalId = generatedId;
        }

        // Check if ExternalId is unique (in case the user provided one)
        if (await _context.Students.AnyAsync(s => s.ExternalId == model.ExternalId))
        {
            ModelState.AddModelError("ExternalId", "This Student ID is already in use.");
            await PopulateAvailableCoursesAsync(model);
            ViewData["Action"] = "Create";
            return View("Form", model);
        }

        var student = new Student
        {
            ExternalId = model.ExternalId,
            FullName = model.FullName,
            Email = model.Email ?? string.Empty,
            Department = model.Department,
            EnrollmentYear = model.EnrollmentYear,
            Courses = await _context.Courses.Where(c => model.SelectedCourseIds.Contains(c.Id)).ToListAsync()
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Student '{student.FullName}' created successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var student = await _context.Students
            .Include(s => s.Courses)
            .FirstOrDefaultAsync(s => s.Id == id);
            
        if (student == null) return NotFound();

        var model = new StudentFormViewModel
        {
            Id = student.Id,
            ExternalId = student.ExternalId,
            FullName = student.FullName,
            Email = student.Email,
            Department = student.Department,
            EnrollmentYear = student.EnrollmentYear,
            SelectedCourseIds = student.Courses.Select(c => c.Id).ToList()
        };

        await PopulateAvailableCoursesAsync(model);
        ViewData["Action"] = "Edit";
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, StudentFormViewModel model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            await PopulateAvailableCoursesAsync(model);
            ViewData["Action"] = "Edit";
            return View("Form", model);
        }

        var student = await _context.Students
            .Include(s => s.Courses)
            .FirstOrDefaultAsync(s => s.Id == id);
            
        if (student == null) return NotFound();

        // Auto-generate ExternalId if left blank during edit
        if (string.IsNullOrWhiteSpace(model.ExternalId))
        {
            var random = new Random();
            string generatedId;
            bool isUnique;
            do
            {
                generatedId = $"STU-{model.EnrollmentYear}-{random.Next(1000, 9999)}";
                isUnique = !await _context.Students.AnyAsync(s => s.ExternalId == generatedId);
            } while (!isUnique);

            model.ExternalId = generatedId;
        }

        // Check uniqueness of ExternalId, excluding current student
        if (await _context.Students.AnyAsync(s => s.ExternalId == model.ExternalId && s.Id != id))
        {
            ModelState.AddModelError("ExternalId", "This Student ID is already in use by another student.");
            await PopulateAvailableCoursesAsync(model);
            ViewData["Action"] = "Edit";
            return View("Form", model);
        }

        student.ExternalId = model.ExternalId;
        student.FullName = model.FullName;
        student.Email = model.Email ?? string.Empty;
        student.Department = model.Department;
        student.EnrollmentYear = model.EnrollmentYear;

        student.Courses.Clear();
        var selectedCourses = await _context.Courses.Where(c => model.SelectedCourseIds.Contains(c.Id)).ToListAsync();
        foreach (var c in selectedCourses) student.Courses.Add(c);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Student '{student.FullName}' updated successfully.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await _context.Students
            .Include(s => s.Fingerprint)
            .FirstOrDefaultAsync(s => s.Id == id);
            
        if (student == null) return NotFound();

        // Check if student has attendance records
        bool hasAttendance = await _context.Attendances.AnyAsync(a => a.StudentId == id);
        if (hasAttendance)
        {
            TempData["ErrorMessage"] = $"Cannot delete student '{student.FullName}' because they have attendance records in the system.";
            return RedirectToAction(nameof(Index));
        }

        // If they have a fingerprint, it cascades or we delete explicitly depending on EF config. 
        // We'll let EF handle cascade delete if configured, or remove explicitly to be safe.
        if (student.Fingerprint != null)
        {
            _context.Fingerprints.Remove(student.Fingerprint);
        }

        _context.Students.Remove(student);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Student '{student.FullName}' deleted successfully.";
        return RedirectToAction(nameof(Index));
    }
}
