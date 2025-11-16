using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MVC_Assignment1.Models;
using MVC_Assignment1.View_Models;

public class ResultsController : Controller
{
    ApplicationDbContext context = new ApplicationDbContext();

    //[Route("Results")]
    public IActionResult Index(int? courseId)
    {
        // Fill dropdown
        ViewBag.Courses = context.Courses.AsNoTracking().ToList();

        if (courseId == null)
        {
            return View(null);
        }

        // Course Data
        var course = context.Courses
             .AsNoTracking()
             .Where(c => c.Id == courseId)
             .Select(c => new CourseResultsVM
             {
                 CourseName = c.Name,
                 MinDegree = c.MinDegree,
                 DepartmentName = c.Department.Name
             })
             .FirstOrDefault();

        if (course == null)
            return NotFound();

        //  Trainees Data
        course.Trainees = context.CrsResults
            .AsNoTracking()
            .Where(r => r.Crs_Id == courseId)
            .Select(r => new TraineeResultVM
            {
                TraineeName = r.Trainee.Name,
                DepartmentName = r.Trainee.Department.Name,
                Degree = r.Degree,
                MinDegree = course.MinDegree,
                Status = r.Degree >= course.MinDegree ? "Pass" : "Fail"
            })
            .ToList();


        return View("Index", course);
    }
}
