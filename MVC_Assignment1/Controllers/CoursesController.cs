using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MVC_Assignment1.Models;
using MVC_Assignment1.View_Models;
using X.PagedList;

namespace MVC_Assignment1.Controllers
{
    public class CoursesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CoursesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ------------------- View Courses with Paging -------------------
        public async Task<IActionResult> Index(int? page)
        {
            int pageSize = 10; // Number of items per page
            int pageNumber = page ?? 1;

            var coursesQuery = _context.Courses
                .AsNoTracking()
                .Include(c => c.Department)
                .OrderBy(c => c.Name)
                .Select(c => new CourseListVM
                {
                    Id = c.Id,
                    Name = c.Name,
                    DepartmentName = c.Department.Name,
                });

            var pagedCourses = await coursesQuery.ToPagedListAsync(pageNumber, pageSize);

            // Display success/error messages if any
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

        // ------------------- View Instructors -------------------
        public async Task<IActionResult> Instructors(int? id, int? page)
        {
            if (id == null) return NotFound();

            // Check if course exists
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == id);
            if (!courseExists) return NotFound();

            int pageSize = 10;
            int pageNumber = page ?? 1;

            // Pass the course ID to the view for pagination
            ViewBag.CourseId = id;

            // Query with Include + Select
            var instructorsQuery = _context.Instructors
                .AsNoTracking()
                .Where(i => i.Crs_Id == id)
                .Include(i => i.Department)
                .Include(i => i.Course)
                .OrderBy(i => i.Name)
                .Select(i => new InstructorListVM
                {
                    Id = i.Id,
                    Name = i.Name,
                    DepartmentName = i.Department.Name,
                    CourseName = i.Course.Name
                });

            var pagedInstructors = await instructorsQuery.ToPagedListAsync(pageNumber, pageSize);

            return View(pagedInstructors);
        }

        // ------------------- Add Course -------------------
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

            try
            {
                var course = new Course
                {
                    Name = vm.Name,
                    Degree = vm.Degree,
                    MinDegree = vm.MinDegree,
                    Dept_Id = vm.DepartmentId
                };

                _context.Add(course);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Course '{course.Name}' has been added successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // Log the exception (you should use a logging framework)
                ModelState.AddModelError("", "Unable to save changes. Please try again. If the problem persists, contact your system administrator.");
                vm.Departments = await GetDepartmentsListAsync();
                return View(vm);
            }
        }

        // ------------------- Edit Course -------------------
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var course = await _context.Courses
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

            if (course == null) return NotFound();

            course.Departments = await GetDepartmentsListAsync();

            return View(course);
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
                if (course == null) return NotFound();

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
                if (!await CourseExistsAsync(vm.Id.Value))
                {
                    return NotFound();
                }
                else
                {
                    ModelState.AddModelError("", "The record you attempted to edit was modified by another user. Please try again.");
                    vm.Departments = await GetDepartmentsListAsync();
                    return View(vm);
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError("", "Unable to save changes. Please try again. If the problem persists, contact your system administrator.");
                vm.Departments = await GetDepartmentsListAsync();
                return View(vm);
            }
        }

        // ------------------- Helper Methods -------------------
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
            return await _context.Courses.AnyAsync(e => e.Id == id);
        }
    }
}