using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeachSpace.Models;
using TeachSpace.View_Models;
using X.PagedList;

namespace TeachSpace.Controllers
{
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ========== INDEX (Paged List) ==========
        public async Task<IActionResult> Index(int? page)
        {
            int pageNumber = page.GetValueOrDefault();
            if (pageNumber < 1)
                pageNumber = 1;

            int pageSize = 10;

            try
            {
                var departmentsQuery = _context.Departments
                    .AsNoTracking()
                    .Select(d => new DepartmentListVM
                    {
                        Id = d.Id,
                        Name = d.Name,
                        Manager = d.Manager
                    })
                    .OrderBy(d => d.Id);

                var pagedDepartments = await departmentsQuery
                    .ToPagedListAsync(pageNumber, pageSize);

                return View(pagedDepartments);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading departments: " + ex.Message;
                var emptyPagedList = new List<DepartmentListVM>()
                    .ToPagedList(pageNumber, pageSize);

                return View(emptyPagedList);
            }
        }



        // ========== ADD (GET) ==========
        [HttpGet]
        public IActionResult Add()
        {
            return View();
        }



        // ========== ADD (POST) ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(DepartmentListVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            try
            {
                var department = new Department
                {
                    Name = vm.Name,
                    Manager = vm.Manager
                };

                _context.Departments.Add(department);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Department added successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "An error occurred: " + ex.Message;
                return View(vm);
            }
        }



        // ========== DETAILS ==========
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var department = await _context.Departments
                .Include(d => d.Instructors)
                    .ThenInclude(i => i.Course)
                .Include(d => d.Trainees)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
                return NotFound();

            var vm = new DepartmentDetailsVM
            {
                Id = department.Id,
                Name = department.Name,
                Manager = department.Manager,
                Instructors = department.Instructors.Select(i => new InstructorVM
                {
                    Id = i.Id,
                    Name = i.Name,
                    CourseName = i.Course.Name,
                    Image = i.Imag
                }).ToList(),

                Trainees = department.Trainees.Select(t => new TraineeVM
                {
                    Id = t.Id,
                    Name = t.Name,
                    Image = t.Imag
                }).ToList()

            };

            return View(vm);
        }





        // ========== EDIT (GET) ==========
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var department = await _context.Departments.FindAsync(id);

            if (department == null)
                return NotFound();

            var vm = new DepartmentListVM
            {
                Id = department.Id,
                Name = department.Name,
                Manager = department.Manager
            };

            return View(vm);
        }



        // ========== EDIT (POST) ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DepartmentListVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var department = await _context.Departments.FindAsync(vm.Id);

            if (department == null)
                return NotFound();

            try
            {
                department.Name = vm.Name;
                department.Manager = vm.Manager;

                _context.Departments.Update(department);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Department updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating department: " + ex.Message;
                return View(vm);
            }
        }



        // ========== DELETE (GET) ==========
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var department = await _context.Departments
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);

            if (department == null)
                return NotFound();

            var vm = new DepartmentListVM
            {
                Id = department.Id,
                Name = department.Name,
                Manager = department.Manager
            };

            return View(vm);
        }



        // ========== DELETE (POST) ==========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);

            if (department == null)
            {
                TempData["ErrorMessage"] = "Department not found.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Departments.Remove(department);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Department deleted successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting department: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
