using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
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

        // ---------------- SMART RETURN (UPDATED) ----------------
        // Renamed parameters to avoid conflict with ViewModel properties
        private IActionResult SmartReturn(string returnTo, int? returnDeptId, int? returnCourseId)
        {
            return returnTo switch
            {
                "Department" => RedirectToAction("Details", "Departments", new { id = returnDeptId }),
                "Course" => RedirectToAction("Instructors", "Courses", new { id = returnCourseId }),
                _ => RedirectToAction(nameof(Index))
            };
        }

        // ---------------- INDEX ----------------
        public async Task<IActionResult> Index(int? page)
        {
            int pageNumber = page.GetValueOrDefault() < 1 ? 1 : page.Value;
            int pageSize = 10;

            var query = _context.Instructors
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

            return View(await query.ToPagedListAsync(pageNumber, pageSize));
        }

        // ---------------- DETAILS ----------------
        public async Task<IActionResult> Details(int id, string returnTo, int? deptId, int? courseId)
        {
            var inst = await _context.Instructors
                .Include(i => i.Department)
                .Include(i => i.Course)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inst == null) return NotFound();

            // Pass original IDs to View for the "Back" button
            ViewBag.ReturnTo = returnTo;
            ViewBag.DepartmentId = deptId;
            ViewBag.CourseId = courseId;

            return View(inst);
        }

        // ---------------- ADD GET ----------------
        [HttpGet]
        public async Task<IActionResult> Add(int? deptId, int? courseId, string returnTo = "Instructors")
        {
            var vm = new InstructorFormVM
            {
                Departments = await _context.Departments
                    .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                    .ToListAsync(),

                Courses = await _context.Courses
                    .Select(x => new SelectListItem { Value = x.Id.ToString(), Text = x.Name })
                    .ToListAsync(),

                // Pre-select values if provided, but these are editable in the dropdown
                DepartmentId = deptId,
                CourseId = courseId
            };

            ViewBag.ReturnTo = returnTo;
            ViewBag.DepartmentId = deptId; // Used for hidden input
            ViewBag.CourseId = courseId;   // Used for hidden input

            return View(vm);
        }

        // ---------------- ADD POST (FIXED) ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        // NOTE: Parameters are named 'returnDeptId' and 'returnCourseId' here
        public async Task<IActionResult> Add(InstructorFormVM vm, string returnTo, int? returnDeptId, int? returnCourseId)
        {
            vm.Departments = await _context.Departments
                .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                .ToListAsync();

            vm.Courses = await _context.Courses
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();

            if (!ModelState.IsValid)
                return View(vm);

            string imageName = "default.png";

            if (vm.UploadImage != null)
            {
                string folder = Path.Combine("wwwroot/images");
                imageName = Guid.NewGuid() + Path.GetExtension(vm.UploadImage.FileName);
                string path = Path.Combine(folder, imageName);

                using var fs = new FileStream(path, FileMode.Create);
                await vm.UploadImage.CopyToAsync(fs);
            }

            var inst = new Instructor
            {
                Name = vm.Name,
                Salary = vm.Salary.Value,
                Address = vm.Address,
                Dept_Id = vm.DepartmentId.Value, // Takes value from Dropdown
                Crs_Id = vm.CourseId.Value,      // Takes value from Dropdown
                Imag = imageName
            };

            _context.Instructors.Add(inst);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Instructor added successfully!";

            // Pass the separate return IDs back to the logic
            return SmartReturn(returnTo, returnDeptId, returnCourseId);
        }

        // ---------------- EDIT (GET) ----------------
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string returnTo, int? deptId, int? courseId)
        {
            var inst = await _context.Instructors.FindAsync(id);
            if (inst == null) return NotFound();

            var vm = new InstructorFormVM
            {
                Id = inst.Id,
                Name = inst.Name,
                Salary = inst.Salary,
                Address = inst.Address,
                DepartmentId = inst.Dept_Id,
                CourseId = inst.Crs_Id,
                ExistingImage = inst.Imag,

                Departments = await _context.Departments
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                    .ToListAsync(),

                Courses = await _context.Courses
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToListAsync()
            };

            ViewBag.ReturnTo = returnTo;
            ViewBag.DepartmentId = deptId;
            ViewBag.CourseId = courseId;

            return View(vm);
        }

        // ---------------- EDIT (POST) - FIX APPLIED HERE ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(InstructorFormVM vm, string returnTo, int? returnDeptId, int? returnCourseId)
        {
            // 1. Check Validation
            if (!ModelState.IsValid)
            {
                // CRITICAL: Reload lists so the dropdowns don't disappear
                vm.Departments = await _context.Departments.Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name }).ToListAsync();
                vm.Courses = await _context.Courses.Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name }).ToListAsync();

                // Restore ViewBags for the hidden inputs
                ViewBag.ReturnTo = returnTo;
                ViewBag.DepartmentId = returnDeptId;
                ViewBag.CourseId = returnCourseId;

                return View(vm);
            }

            // 2. Retrieve Instructor
            var inst = await _context.Instructors.FindAsync(vm.Id);
            if (inst == null) return NotFound();

            // 3. Handle Image Upload
            if (vm.UploadImage != null)
            {
                string folder = Path.Combine("wwwroot/images");
                string imageName = Guid.NewGuid() + Path.GetExtension(vm.UploadImage.FileName);
                string path = Path.Combine(folder, imageName);

                using var fs = new FileStream(path, FileMode.Create);
                await vm.UploadImage.CopyToAsync(fs);

                inst.Imag = imageName; // Update DB with new name
            }

            // 4. Update Properties
            inst.Name = vm.Name;
            inst.Salary = vm.Salary.Value;
            inst.Address = vm.Address;
            inst.Dept_Id = vm.DepartmentId.Value;
            inst.Crs_Id = vm.CourseId.Value;

            // 5. Save
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Instructor updated successfully!";
            return SmartReturn(returnTo, returnDeptId, returnCourseId);
        }

        // ---------------- DELETE (GET) ----------------
        [HttpGet]
        public async Task<IActionResult> Delete(int id, string returnTo, int? deptId, int? courseId)
        {
            var inst = await _context.Instructors
                .Include(i => i.Department)
                .Include(i => i.Course)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inst == null) return NotFound();

            ViewBag.ReturnTo = returnTo;
            ViewBag.DepartmentId = deptId;
            ViewBag.CourseId = courseId;

            return PartialView("_DeleteInstructorModal", inst);
        }

        // ---------------- DELETE (POST) ----------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string returnTo, int? returnDeptId, int? returnCourseId)
        {
            var inst = await _context.Instructors.FindAsync(id);
            if (inst == null) return NotFound();

            _context.Instructors.Remove(inst);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Instructor deleted successfully!";
            return SmartReturn(returnTo, returnDeptId, returnCourseId);
        }
    }
}