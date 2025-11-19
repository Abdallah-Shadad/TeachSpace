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

        // ------------------- 1. List Trainees in a Course -------------------
        public async Task<IActionResult> Index(int id, int? page)
        {
            var course = await _context.Courses
                .Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return NotFound();

            int pageSize = 10;
            int pageNumber = page ?? 1;

            var resultsQuery = _context.CrsResults
                .Include(r => r.Trainee)
                .Where(r => r.Crs_Id == id)
                .Select(r => new TraineeResultVM
                {
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

        // ------------------- 2. Add Existing Trainee (GET) -------------------
        [HttpGet]
        // FIX: Renamed from 'AddTrainee' to 'Add' to match Add.cshtml
        public async Task<IActionResult> Add(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return NotFound();

            // 1. Find IDs of trainees ALREADY in this course
            var existingIds = await _context.CrsResults
                .Where(r => r.Crs_Id == courseId)
                .Select(r => r.Trainee_Id)
                .ToListAsync();

            // 2. Get Trainees who are NOT in the list above
            var availableTrainees = await _context.Trainees
                .Where(t => !existingIds.Contains(t.Id))
                .Select(t => new SelectListItem
                {
                    Value = t.Id.ToString(),
                    Text = t.Name
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

        // ------------------- 3. Add Existing Trainee (POST) -------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        // FIX: Renamed from 'AddTrainee' to 'Add' to match <form asp-action="Add">
        public async Task<IActionResult> Add(CourseRegistrationVM vm)
        {
            if (vm.TraineeId == 0)
            {
                ModelState.AddModelError("TraineeId", "Please select a trainee.");
            }

            if (!ModelState.IsValid)
            {
                var existingIds = await _context.CrsResults
                    .Where(r => r.Crs_Id == vm.CourseId)
                    .Select(r => r.Trainee_Id)
                    .ToListAsync();

                vm.AvailableTrainees = await _context.Trainees
                    .Where(t => !existingIds.Contains(t.Id))
                    .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Name })
                    .ToListAsync();

                return View(vm);
            }

            var result = new CrsResult
            {
                Crs_Id = vm.CourseId,
                Trainee_Id = vm.TraineeId,
                Degree = 0
            };

            _context.CrsResults.Add(result);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Trainee added to course successfully!";
            return RedirectToAction("Index", new { id = vm.CourseId });
        }
        // ------------------- 4. Edit Degree (GET) -------------------
        [HttpGet]
        public async Task<IActionResult> EditDegree(int courseId, int traineeId)
        {
            // Find the specific link record
            var result = await _context.CrsResults
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
                Degree = result.Degree
            };

            return View(vm);
        }

        // ------------------- 5. Edit Degree (POST) -------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDegree(EditDegreeVM vm)
        {
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
