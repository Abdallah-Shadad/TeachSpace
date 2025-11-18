using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeachSpace.Models;
using TeachSpace.View_Models;
using X.PagedList;

public class ResultsController : Controller
{
    // Use Dependency Injection 
    private readonly ApplicationDbContext _context;

    public ResultsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(int? courseId, int? page)
    {
        // Define page size and number
        int pageSize = 20;
        int pageNumber = (page ?? 1);

        // Fill dropdown
        ViewBag.Courses = await _context.Courses
            .AsNoTracking()
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        if (courseId == null)
            return View(null);

        // Course Data 
        var course = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new CourseResultsVM
            {
                CourseName = c.Name,
                MinDegree = c.MinDegree,
                DepartmentName = c.Department != null ? c.Department.Name : "N/A"
            })
            .FirstOrDefaultAsync();

        if (course == null)
            return NotFound();

        // Build the query for Trainees
        var traineesQuery = _context.CrsResults
            .AsNoTracking()
            .Where(r => r.Crs_Id == courseId)
            .OrderBy(r => r.Trainee.Dept_Id)
            .ThenBy(r => r.Trainee.Name)
            .Select(r => new TraineeResultVM
            {
                TraineeName = r.Trainee.Name,
                DepartmentName = r.Trainee.Department != null ? r.Trainee.Department.Name : "N/A",
                Degree = r.Degree,
                MinDegree = course.MinDegree,
                Status = r.Degree >= course.MinDegree ? "Pass" : "Fail"
            });

        course.Trainees = await traineesQuery.ToPagedListAsync(pageNumber, pageSize);

        return View("Index", course);
    }
}