using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TeachSpace.Models;
using TeachSpace.View_Models;
using X.PagedList;

namespace TeachSpace.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // COURSES LIST (GLOBAL) WITH PAGING
        // =====================================================
        public async Task<IActionResult> Index(int? page)
        {
            int pageNumber = page.GetValueOrDefault() < 1 ? 1 : page.Value;
            int pageSize = 10;

            var coursesQuery = _context.Courses
                .AsNoTracking()
                .Include(c => c.Department)
                .OrderBy(c => c.Name)
                .Select(c => new CourseListVM
                {
                    Id = c.Id,
                    Name = c.Name,
                    DepartmentName = c.Department.Name
                });

            var pagedCourses = await coursesQuery.ToPagedListAsync(pageNumber, pageSize);

            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }

            if (TempData["ErrorMessage"] != null)
            {
                ViewBag.ErrorMessage = TempData["ErrorMessage"];
            }

            return View(pagedCourses);
        }

        // =====================================================
        // INSTRUCTORS OF A SPECIFIC COURSE (CHILD PAGE)
        // This is the "course context" used with SmartBack = "Course"
        // =====================================================
        public async Task<IActionResult> Instructors(int id, int? page)
        {
            var course = await _context.Courses
                .AsNoTracking()
                .Include(c => c.Department)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null)
                return NotFound();

            int pageNumber = page.GetValueOrDefault() < 1 ? 1 : page.Value;
            int pageSize = 10;

            ViewBag.CourseId = id;
            ViewBag.CourseName = course.Name;
            ViewBag.DepartmentName = course.Department?.Name;

            var instructorsQuery = _context.Instructors
                .AsNoTracking()
                .Where(i => i.Crs_Id == id)
                .Include(i => i.Department)
                .OrderBy(i => i.Name)
                .Select(i => new InstructorListVM
                {
                    Id = i.Id,
                    Name = i.Name,
                    DepartmentName = i.Department.Name,
                    CourseName = course.Name
                });

            var pagedInstructors = await instructorsQuery.ToPagedListAsync(pageNumber, pageSize);

            return View(pagedInstructors);
        }

        // =====================================================
        // ADD COURSE (STEP 1 IN WIZARD)
        // Step 2 is InstructorsController.AddFirstInstructor
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var vm = new CourseFormVM
            {
                Departments = await GetDepartmentsListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(CourseFormVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Departments = await GetDepartmentsListAsync();
                return View(vm);
            }

            // Serialize course data to TempData for step 2 (AddFirstInstructor)
            string serializedCourse = JsonSerializer.Serialize(vm);
            TempData["PendingCourseData"] = serializedCourse;

            // Redirect to Step 2 (wizard)
            return RedirectToAction("AddFirstInstructor", "Instructors");
        }

        // =====================================================
        // EDIT COURSE
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var courseVm = await _context.Courses
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CourseFormVM
                {
                    Id = c.Id,
                    Name = c.Name,
                    Degree = c.Degree,
                    MinDegree = c.MinDegree,
                    DepartmentId = c.Dept_Id
                })
                .FirstOrDefaultAsync();

            if (courseVm == null)
                return NotFound();

            courseVm.Departments = await GetDepartmentsListAsync();

            return View(courseVm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CourseFormVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Departments = await GetDepartmentsListAsync();
                return View(vm);
            }

            try
            {
                var course = await _context.Courses.FindAsync(vm.Id);
                if (course == null)
                    return NotFound();

                course.Name = vm.Name;
                course.Degree = vm.Degree;
                course.MinDegree = vm.MinDegree;
                course.Dept_Id = vm.DepartmentId;

                _context.Update(course);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Course '{course.Name}' has been updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await CourseExistsAsync(vm.Id ?? 0))
                    return NotFound();

                ModelState.AddModelError(string.Empty, "The record was modified by another user. Please reload and try again.");
                vm.Departments = await GetDepartmentsListAsync();
                return View(vm);
            }
        }

        // =====================================================
        // HELPERS
        // =====================================================
        private async Task<List<SelectListItem>> GetDepartmentsListAsync()
        {
            return await _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .Select(d => new SelectListItem
                {
                    Value = d.Id.ToString(),
                    Text = d.Name
                })
                .ToListAsync();
        }

        private async Task<bool> CourseExistsAsync(int id)
        {
            return await _context.Courses.AnyAsync(c => c.Id == id);
        }
    }
}
