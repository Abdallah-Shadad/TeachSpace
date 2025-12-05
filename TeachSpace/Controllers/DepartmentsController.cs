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

        // ==========================================
        // REMOTE VALIDATION ACTION (AJAX)
        // ==========================================
        [AcceptVerbs("GET", "POST")]
        public IActionResult CheckName(string Name, int? Id)
        {
            // Check if name exists, EXCLUDING the current ID (for Edit scenarios)
            bool exists = _context.Departments.Any(d => d.Name == Name && d.Id != (Id ?? 0));

            // Remote attribute expects JSON: true = valid, false = invalid
            return Json(!exists);
        }

        // ==========================================
        // INDEX
        // ==========================================
        public async Task<IActionResult> Index(int? page)
        {
            int pageNumber = page.GetValueOrDefault() < 1 ? 1 : page.Value;
            int pageSize = 10;

            var departments = _context.Departments
                .AsNoTracking()
                .OrderBy(d => d.Id)
                .Select(d => new DepartmentListVM
                {
                    Id = d.Id,
                    Name = d.Name,
                    Manager = d.Manager
                });

            return View(await departments.ToPagedListAsync(pageNumber, pageSize));
        }

        // ==========================================
        // ADD (Create)
        // ==========================================
        [HttpGet]
        public IActionResult Add()
        {
            return View(new DepartmentFormVM());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(DepartmentFormVM vm)
        {
            // A. Manual Server-Side Check (Security Net)
            if (_context.Departments.Any(d => d.Name == vm.Name))
            {
                ModelState.AddModelError("Name", "Department Name already exists!");
            }

            // B. Standard Validation
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            try
            {
                // Mapping: ViewModel -> Entity
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
                TempData["ErrorMessage"] = "Error adding department: " + ex.Message;
                return View(vm);
            }
        }

        // ==========================================
        // EDIT
        // ==========================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department == null) return NotFound();

            // Mapping: Entity -> ViewModel
            var vm = new DepartmentFormVM
            {
                Id = department.Id,
                Name = department.Name,
                Manager = department.Manager
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(DepartmentFormVM vm)
        {
            // A. Manual Check (Exclude current ID)
            if (_context.Departments.Any(d => d.Name == vm.Name && d.Id != vm.Id))
            {
                ModelState.AddModelError("Name", "Department Name already exists!");
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            try
            {
                var department = await _context.Departments.FindAsync(vm.Id);
                if (department == null) return NotFound();

                // Update properties
                department.Name = vm.Name;
                department.Manager = vm.Manager;

                _context.Update(department);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Department updated successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating: " + ex.Message;
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
    }
}