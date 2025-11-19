using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json; // Required for the wizard step
using TeachSpace.Models;
using TeachSpace.View_Models;
using X.PagedList;

namespace TeachSpace.Controllers
{
    public class InstructorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InstructorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ------------------- View Instructors (Global List) -------------------
        public async Task<IActionResult> Index(int? page)
        {
            int pageSize = 10;
            int pageNumber = (page ?? 1);
            try
            {
                var instructorsQuery = _context.Instructors
                    .AsNoTracking()
                    .Include(i => i.Department)
                    .Include(i => i.Course)
                    .Select(i => new InstructorListVM
                    {
                        Id = i.Id,
                        Name = i.Name,
                        DepartmentName = i.Department.Name,
                        CourseName = i.Course.Name
                    })
                    .OrderBy(i => i.Name);

                var pagedInstructors = await instructorsQuery
                    .ToPagedListAsync(pageNumber, pageSize);
                return View(pagedInstructors);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading instructors: " + ex.Message;
                var emptyPagedList = new List<InstructorListVM>().ToPagedList(pageNumber, pageSize);
                return View(emptyPagedList);
            }
        }

        // ------------------- View Instructors Detail (Single Course) -------------------
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var instructor = await _context.Instructors
                .AsNoTracking()
                .Include(i => i.Department)
                .Include(i => i.Course)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (instructor == null) return NotFound();

            return View(instructor);
        }

        // =========================================================================
        // WIZARD STEP 2: ADD FIRST INSTRUCTOR (Creates Course + Instructor)
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> AddFirstInstructor()
        {
            // 1. Check if we have Course Data from Step 1
            if (TempData["PendingCourseData"] == null)
            {
                TempData["ErrorMessage"] = "Session expired. Please start creating the course again.";
                return RedirectToAction("Add", "Courses");
            }

            // 2. Keep data for the next request
            TempData.Keep("PendingCourseData");

            var vm = new InstructorFormVM
            {
                Departments = await _context.Departments
                    .AsNoTracking()
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                    .ToListAsync()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFirstInstructor(InstructorFormVM vm)
        {
            // 1. Retrieve Course Data
            string? courseJson = TempData["PendingCourseData"] as string;

            if (string.IsNullOrEmpty(courseJson))
            {
                return RedirectToAction("Add", "Courses");
            }

            if (!ModelState.IsValid)
            {
                TempData.Keep("PendingCourseData"); // Don't lose the course data!
                vm.Departments = await _context.Departments
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                    .ToListAsync();
                return View(vm);
            }

            try
            {
                // 2. Deserialize Course
                var courseVm = JsonSerializer.Deserialize<CourseFormVM>(courseJson);

                // 3. Prepare Course Entity
                var course = new Course
                {
                    Name = courseVm.Name,
                    Degree = courseVm.Degree,
                    MinDegree = courseVm.MinDegree,
                    Dept_Id = courseVm.DepartmentId,

                    // 4. MAGIC: Add Instructor to the Course's list
                    // EF Core saves both in one transaction
                    Instructors = new List<Instructor>
                    {
                        new Instructor
                        {
                            Name = vm.Name,
                            Salary = vm.Salary,
                            Address = vm.Address,
                            Dept_Id = vm.DepartmentId,
                            Imag = "default.png"
                        }
                    }
                };

                // 5. Save Everything
                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Course and First Instructor added successfully!";

                // Redirect to the Course's instructor list
                return RedirectToAction("Instructors", "Courses", new { id = course.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Error saving data: " + ex.Message);
                TempData.Keep("PendingCourseData");
                vm.Departments = await _context.Departments
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                    .ToListAsync();
                return View(vm);
            }
        }

        // =========================================================================
        // STANDARD ADD (For adding 2nd, 3rd instructor to EXISTING course)
        // =========================================================================

        [HttpGet]
        public async Task<IActionResult> Add(int? courseId)
        {
            var vm = new InstructorFormVM
            {
                Departments = await _context.Departments
                    .AsNoTracking()
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                    .ToListAsync(),

                Courses = await _context.Courses
                    .AsNoTracking()
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToListAsync()
            };

            if (courseId != null)
            {
                vm.CourseId = courseId.Value;
                var course = await _context.Courses.FindAsync(courseId);
                if (course != null) vm.DepartmentId = course.Dept_Id;
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(InstructorFormVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Departments = await _context.Departments
                    .AsNoTracking()
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                    .ToListAsync();
                vm.Courses = await _context.Courses
                    .AsNoTracking()
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToListAsync();
                return View(vm);
            }

            string imageName = "default.png";
            if (vm.UploadImage != null)
            {
                string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
                imageName = Guid.NewGuid() + Path.GetExtension(vm.UploadImage.FileName);
                string filePath = Path.Combine(uploadsFolder, imageName);
                using var fs = new FileStream(filePath, FileMode.Create);
                await vm.UploadImage.CopyToAsync(fs);
            }

            var instructor = new Instructor
            {
                Name = vm.Name,
                Salary = vm.Salary,
                Address = vm.Address,
                Dept_Id = vm.DepartmentId,
                Crs_Id = vm.CourseId,
                Imag = imageName
            };

            _context.Instructors.Add(instructor);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Instructor added successfully!";
            return RedirectToAction("Instructors", "Courses", new { id = vm.CourseId });
        }

        // ------------------- Edit Instructor -------------------
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            try
            {
                var instructor = await _context.Instructors
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (instructor == null) return NotFound();

                var vm = new InstructorFormVM
                {
                    Id = instructor.Id,
                    Name = instructor.Name,
                    Departments = await _context.Departments
                        .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                        .ToListAsync(),
                    Courses = await _context.Courses
                        .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                        .ToListAsync(),
                    DepartmentId = instructor.Dept_Id,
                    CourseId = instructor.Crs_Id
                };

                return View("Edit", vm);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading instructor: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InstructorFormVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Departments = await _context.Departments
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                    .ToListAsync();

                vm.Courses = await _context.Courses
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToListAsync();

                return View(vm);
            }
            var instructor = await _context.Instructors.FindAsync(vm.Id);
            if (instructor == null) return NotFound();

            instructor.Name = vm.Name;
            instructor.Dept_Id = vm.DepartmentId;
            instructor.Crs_Id = vm.CourseId;

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}