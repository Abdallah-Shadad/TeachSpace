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

        // ============================================================
        // ⭐ SMART RETURN LOGIC
        // This method decides where to redirect the user after
        // Add/Edit/Delete/Details based on where they came from.
        // ============================================================
        private IActionResult SmartReturn(string returnTo, int? deptId, int? courseId)
        {
            return returnTo switch
            {
                "Department" =>
                    RedirectToAction("Details", "Departments", new { id = deptId }),

                "Course" =>
                    RedirectToAction("Instructors", "Courses", new { id = courseId }),

                _ =>
                    RedirectToAction(nameof(Index))
            };
        }

        // ============================================================
        // ⭐ REGION: GLOBAL INSTRUCTORS LIST
        // ============================================================
        #region Index

        public async Task<IActionResult> Index(int? page)
        {
            int pageNumber = page.GetValueOrDefault() < 1 ? 1 : page.Value;
            int pageSize = 10;

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

            var pagedList = await instructorsQuery.ToPagedListAsync(pageNumber, pageSize);

            return View(pagedList);
        }

        #endregion

        // ============================================================
        // ⭐ REGION: DETAILS
        // Supports Smart Back navigation
        // ============================================================
        #region Details

        public async Task<IActionResult> Details(int id, string returnTo, int? deptId, int? courseId)
        {
            var instructor = await _context.Instructors
                .Include(i => i.Department)
                .Include(i => i.Course)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (instructor == null)
                return NotFound();

            // Pass navigation context to the View
            ViewBag.ReturnTo = returnTo;
            ViewBag.DepartmentId = deptId;
            ViewBag.CourseId = courseId;

            return View(instructor);
        }

        #endregion

        // ============================================================
        // ⭐ REGION: ADD INSTRUCTOR
        // Supports: Add by Department, Add by Course, Add from anywhere
        // ============================================================
        #region Add

        [HttpGet]
        public async Task<IActionResult> Add(int? deptId, int? courseId, string returnTo = "Instructors")
        {
            var vm = new InstructorFormVM
            {
                Departments = await _context.Departments
                    .Select(d => new SelectListItem { Value = d.Id.ToString(), Text = d.Name })
                    .ToListAsync(),

                Courses = await _context.Courses
                    .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                    .ToListAsync(),

                DepartmentId = deptId ?? 0,
                CourseId = courseId ?? 0
            };

            // Pass smart-back context
            ViewBag.ReturnTo = returnTo;
            ViewBag.DepartmentId = deptId;
            ViewBag.CourseId = courseId;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
            InstructorFormVM vm,
            string returnTo,
            int? deptId,
            int? courseId)
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

            string imageName = "default.png";

            if (vm.UploadImage != null)
            {
                string uploadsFolder = Path.Combine("wwwroot/images");
                imageName = Guid.NewGuid() + Path.GetExtension(vm.UploadImage.FileName);
                string imagePath = Path.Combine(uploadsFolder, imageName);

                using var fs = new FileStream(imagePath, FileMode.Create);
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

            return SmartReturn(returnTo, deptId, courseId);
        }

        #endregion

        // ============================================================
        // ⭐ REGION: EDIT INSTRUCTOR
        // Fully supports Smart Back navigation
        // ============================================================
        #region Edit

        [HttpGet]
        public async Task<IActionResult> Edit(int id, string returnTo, int? deptId, int? courseId)
        {
            var instructor = await _context.Instructors.FindAsync(id);

            if (instructor == null)
                return NotFound();

            var vm = new InstructorFormVM
            {
                Id = instructor.Id,
                Name = instructor.Name,
                Salary = instructor.Salary,
                Address = instructor.Address,
                DepartmentId = instructor.Dept_Id,
                CourseId = instructor.Crs_Id,

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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            InstructorFormVM vm,
            string returnTo,
            int? deptId,
            int? courseId)
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

            if (instructor == null)
                return NotFound();

            instructor.Name = vm.Name;
            instructor.Salary = vm.Salary;
            instructor.Address = vm.Address;
            instructor.Dept_Id = vm.DepartmentId;
            instructor.Crs_Id = vm.CourseId;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Instructor updated successfully!";

            return SmartReturn(returnTo, deptId, courseId);
        }

        #endregion

        // ============================================================
        // ⭐ REGION: DELETE INSTRUCTOR
        // ============================================================
        #region DELETE (with SmartBack)

        [HttpGet]
        public async Task<IActionResult> Delete(int id, string returnTo, int? deptId, int? courseId)
        {
            var instructor = await _context.Instructors
                .Include(i => i.Department)
                .Include(i => i.Course)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (instructor == null)
                return NotFound();

            // Pass smart back
            ViewBag.ReturnTo = returnTo;
            ViewBag.DepartmentId = deptId;
            ViewBag.CourseId = courseId;

            return PartialView("_DeleteInstructorModal", instructor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(
            int id, string returnTo, int? deptId, int? courseId)
        {
            var instructor = await _context.Instructors.FindAsync(id);
            if (instructor == null)
                return NotFound();

            _context.Instructors.Remove(instructor);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Instructor deleted successfully!";

            return SmartReturn(returnTo, deptId, courseId);
        }

        #endregion

    }
}
