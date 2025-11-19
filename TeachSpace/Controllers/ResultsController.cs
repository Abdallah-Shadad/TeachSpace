using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        int pageSize = 20;
        int pageNumber = (page ?? 1);

        // Fill dropdown for the filter
        ViewBag.Courses = await _context.Courses
            .AsNoTracking()
            .Select(c => new { c.Id, c.Name })
            .ToListAsync();

        if (courseId == null) return View(null);

        // 1. Get Course Info
        var course = await _context.Courses
            .AsNoTracking()
            .Where(c => c.Id == courseId)
            .Select(c => new CourseResultsVM
            {
                CourseName = c.Name,
                MinDegree = c.MinDegree,
                DepartmentName = c.Department.Name
            })
            .FirstOrDefaultAsync();

        if (course == null) return NotFound();

        // 2. Get Trainees
        var traineesQuery = _context.CrsResults
            .AsNoTracking()
            .Include(r => r.Trainee)
            .Where(r => r.Crs_Id == courseId)
            .OrderBy(r => r.Trainee.Name)
            .Select(r => new TraineeResultVM
            {
                TraineeId = r.Trainee_Id,
                TraineeName = r.Trainee.Name,
                Image = r.Trainee.Imag,
                Degree = r.Degree
            });

        course.Trainees = await traineesQuery.ToPagedListAsync(pageNumber, pageSize);

        // Pass CourseId for the "Add Trainee" button
        ViewBag.CourseId = courseId;

        return View(course);
    }

    // ------------------- 2. Add Existing Trainee (GET) -------------------
    [HttpGet]
    public async Task<IActionResult> AddTrainee(int courseId)
    {
        var course = await _context.Courses.FindAsync(courseId);
        if (course == null) return NotFound();

        // 1. Find IDs of trainees ALREADY in this course
        var existingTraineeIds = await _context.CrsResults
            .Where(r => r.Crs_Id == courseId)
            .Select(r => r.Trainee_Id)
            .ToListAsync();

        // 2. Get Trainees NOT in the list, with extra details for the dropdown
        var availableTrainees = await _context.Trainees
            .AsNoTracking()
            .Include(t => t.Department) // <--- CRITICAL: Include Department so we can show its name
            .Where(t => !existingTraineeIds.Contains(t.Id))
            .OrderBy(t => t.Name)
            .Select(t => new SelectListItem
            {
                Value = t.Id.ToString(),

                // --- THE FIX: Unique Display Text ---
                // Example Output: "Ahmed Ali (CS Department) #15"
                Text = $"{t.Name} ({t.Department.Name}) - #{t.Id}"
            })
            .ToListAsync();

        var vm = new CourseRegistrationVM
        {
            CourseId = courseId,
            CourseName = course.Name,
            AvailableTrainees = availableTrainees
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTrainee(CourseRegistrationVM vm)
    {
        if (vm.TraineeId == 0) ModelState.AddModelError("TraineeId", "Select a trainee");

        // 1. Validation: Check Max Degree
        var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == vm.CourseId);

        if (course != null && vm.Degree > course.Degree)
        {
            ModelState.AddModelError("Degree", $"Grade cannot be higher than {course.Degree} for this course.");
        }

        // 2. Validation: Check Duplicates
        // Note: I used 'Trainee_Id' here to match your object creation below. 
        // Ensure your CrsResult model uses 'Trainee_Id' and not 'Tra_Id'.
        bool alreadyExists = await _context.CrsResults.AnyAsync(r => r.Crs_Id == vm.CourseId && r.Trainee_Id == vm.TraineeId);

        if (alreadyExists)
        {
            ModelState.AddModelError("TraineeId", "This trainee is already registered.");
        }

        if (!ModelState.IsValid)
        {
            // --- CRITICAL FIX: RELOAD THE LIST ---
            // We must fetch the list again so the dropdown is not empty on error.

            var existingIds = await _context.CrsResults
                .Where(r => r.Crs_Id == vm.CourseId)
                .Select(r => r.Trainee_Id)
                .ToListAsync();

            vm.AvailableTrainees = await _context.Trainees
                .Include(t => t.Department) // Include Dept for the nice display text
                .Where(t => !existingIds.Contains(t.Id))
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    // Unique Display Text: Name (Dept) - #ID
                    Text = $"{t.Name} ({t.Department.Name}) - #{t.Id}"
                })
                .ToListAsync();

            return View(vm);
        }

        // 3. Save
        var result = new CrsResult
        {
            Crs_Id = vm.CourseId,
            Trainee_Id = vm.TraineeId,
            Degree = vm.Degree
        };

        _context.CrsResults.Add(result);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Trainee added to course successfully!";
        return RedirectToAction(nameof(Index), new { courseId = vm.CourseId });
    }
    // ------------------- 3. Edit Degree (GET) -------------------
    [HttpGet]
    public async Task<IActionResult> EditDegree(int courseId, int traineeId)
    {
        var result = await _context.CrsResults
            .Include(r => r.Trainee)
            .Include(r => r.Course) // Make sure Course is included
            .FirstOrDefaultAsync(r => r.Crs_Id == courseId && r.Trainee_Id == traineeId);

        if (result == null) return NotFound();

        var vm = new EditDegreeVM
        {
            CourseId = result.Crs_Id,
            TraineeId = result.Trainee_Id,
            TraineeName = result.Trainee.Name,
            CourseName = result.Course.Name,
            Degree = result.Degree,

            // NEW: Pass the specific course's max degree
            MaxDegree = result.Course.Degree
        };

        return View(vm);
    }

    // ------------------- 4. Edit Degree (POST) -------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDegree(EditDegreeVM vm)
    {
        // 1. Fetch the Course to check the Max Degree rule
        var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == vm.CourseId);

        if (course != null && vm.Degree > course.Degree)
        {
            // Add a custom error if the rule is broken
            ModelState.AddModelError("Degree", $"Degree cannot exceed the Course Maximum of {course.Degree}.");
        }

        if (!ModelState.IsValid)
        {
            // Be sure to re-populate MaxDegree if we return to the view!
            if (course != null) vm.MaxDegree = course.Degree;
            return View(vm);
        }

        var result = await _context.CrsResults
            .FirstOrDefaultAsync(r => r.Crs_Id == vm.CourseId && r.Trainee_Id == vm.TraineeId);

        if (result == null) return NotFound();

        result.Degree = vm.Degree;

        _context.Update(result);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Degree updated successfully!";
        return RedirectToAction(nameof(Index), new { courseId = vm.CourseId });
    }
}
