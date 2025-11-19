using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TeachSpace.Models;
using TeachSpace.View_Models;
using X.PagedList;

namespace TeachSpace.Controllers
{
    public class CourseTraineesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourseTraineesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // -------------------------------------------------------------------------
        // 1. Index: List Trainees in a Course
        // -------------------------------------------------------------------------
        public async Task<IActionResult> Index(int id, int? page)
        {
            var course = await _context.Courses
                .Include(c => c.Department)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            int pageSize = 10;
            int pageNumber = page ?? 1;

            // Query the CrsResults table to find trainees in this course
            var resultsQuery = _context.CrsResults
                .AsNoTracking()
                .Include(r => r.Trainee)
                .Where(r => r.Crs_Id == id)
                .Select(r => new TraineeResultVM
                {
                    // Note: Using 'Trainee_Id' based on your database schema
                    TraineeId = r.Trainee_Id,
                    TraineeName = r.Trainee.Name,
                    Image = r.Trainee.Imag,
                    Degree = r.Degree
                })
                .OrderBy(t => t.TraineeName);

            var pagedResults = await resultsQuery.ToPagedListAsync(pageNumber, pageSize);

            var vm = new CourseResultsVM
            {
                CourseName = course.Name,
                MinDegree = course.MinDegree,
                DepartmentName = course.Department.Name,
                Trainees = pagedResults
            };

            ViewBag.CourseId = id;
            return View(vm);
        }

        // -------------------------------------------------------------------------
        // 2. Add: Register Existing Trainee (GET)
        // -------------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Add(int courseId)
        {
            var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == courseId);
            if (course == null) return NotFound();

            // 1. Find IDs of trainees ALREADY in this course to exclude them
            var existingIds = await _context.CrsResults
                .Where(r => r.Crs_Id == courseId)
                .Select(r => r.Trainee_Id)
                .ToListAsync();

            // 2. Get Trainees who are NOT in the list above
            var availableTrainees = await _context.Trainees
                .AsNoTracking()
                .Include(t => t.Department) // Include Dept for better display text
                .Where(t => !existingIds.Contains(t.Id))
                .OrderBy(t => t.Name)
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    // Display: "Ahmed (CS Dept) - #10"
                    Text = $"{t.Name} ({t.Department.Name}) - #{t.Id}"
                })
                .ToListAsync();

            var vm = new CourseRegistrationVM
            {
                CourseId = courseId,
                CourseName = course.Name,
                MaxDegree = course.Degree, // Pass the limit to the view
                AvailableTrainees = availableTrainees
            };

            return View(vm);
        }

        // -------------------------------------------------------------------------
        // 3. Add: Register Existing Trainee (POST)
        // -------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(CourseRegistrationVM vm)
        {
            if (vm.TraineeId == 0) ModelState.AddModelError("TraineeId", "Please select a trainee.");

            // 1. Fetch Course to validate Degree Limit
            var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == vm.CourseId);

            if (course != null && vm.Degree > course.Degree)
            {
                ModelState.AddModelError("Degree", $"Grade cannot be higher than {course.Degree} for this course.");
                vm.MaxDegree = course.Degree; // Restore max degree for the view
            }

            // 2. Validation: Check Duplicates
            bool alreadyExists = await _context.CrsResults.AnyAsync(r => r.Crs_Id == vm.CourseId && r.Trainee_Id == vm.TraineeId);
            if (alreadyExists)
            {
                ModelState.AddModelError("TraineeId", "This trainee is already registered.");
            }

            if (!ModelState.IsValid)
            {
                // Reload list on error
                var existingIds = await _context.CrsResults
                    .Where(r => r.Crs_Id == vm.CourseId)
                    .Select(r => r.Trainee_Id)
                    .ToListAsync();

                vm.AvailableTrainees = await _context.Trainees
                    .AsNoTracking()
                    .Include(t => t.Department)
                    .Where(t => !existingIds.Contains(t.Id))
                    .Select(t => new SelectListItem
                    {
                        Value = t.Id.ToString(),
                        Text = $"{t.Name} ({t.Department.Name}) - #{t.Id}"
                    })
                    .ToListAsync();

                // Ensure MaxDegree is set if course was found
                if (course != null) vm.MaxDegree = course.Degree;

                return View(vm);
            }

            // 3. Save the Link and Grade
            var result = new CrsResult
            {
                Crs_Id = vm.CourseId,
                Trainee_Id = vm.TraineeId,
                Degree = vm.Degree // Save initial grade
            };

            _context.CrsResults.Add(result);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Trainee added to course successfully!";
            return RedirectToAction("Index", new { id = vm.CourseId });
        }

        // -------------------------------------------------------------------------
        // 4. Edit Degree (GET)
        // -------------------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> EditDegree(int courseId, int traineeId)
        {
            var result = await _context.CrsResults
                .AsNoTracking()
                .Include(r => r.Trainee)
                .Include(r => r.Course)
                .FirstOrDefaultAsync(r => r.Crs_Id == courseId && r.Trainee_Id == traineeId);

            if (result == null) return NotFound();

            var vm = new EditDegreeVM
            {
                CourseId = result.Crs_Id,
                TraineeId = result.Trainee_Id,
                TraineeName = result.Trainee.Name,
                CourseName = result.Course.Name,
                Degree = result.Degree,
                MaxDegree = result.Course.Degree // Pass limit to view
            };

            return View(vm);
        }

        // -------------------------------------------------------------------------
        // 5. Edit Degree (POST)
        // -------------------------------------------------------------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDegree(EditDegreeVM vm)
        {
            // 1. Fetch Course to validate Degree Limit
            var course = await _context.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == vm.CourseId);

            if (course != null && vm.Degree > course.Degree)
            {
                ModelState.AddModelError("Degree", $"Grade cannot exceed {course.Degree}.");
                vm.MaxDegree = course.Degree; // Restore for View
            }

            if (!ModelState.IsValid) return View(vm);

            var result = await _context.CrsResults
                .FirstOrDefaultAsync(r => r.Crs_Id == vm.CourseId && r.Trainee_Id == vm.TraineeId);

            if (result == null) return NotFound();

            // Update the degree
            result.Degree = vm.Degree;

            _context.Update(result);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Degree for {vm.TraineeName} updated successfully!";
            return RedirectToAction("Index", new { id = vm.CourseId });
        }
    }
}