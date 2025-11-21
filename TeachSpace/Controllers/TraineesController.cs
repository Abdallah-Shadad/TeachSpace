using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TeachSpace.Models;
using TeachSpace.View_Models;
using X.PagedList;

namespace TeachSpace.Controllers
{
    public class TraineesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        public TraineesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // =============================================================
        // SMART BACK: decide where to go after Add/Edit
        // =============================================================
        private IActionResult SmartReturn(string returnTo, int? deptId, int? courseId)
        {
            return returnTo switch
            {
                "Department" => RedirectToAction("Details", "Departments", new { id = deptId }),
                "Course" => RedirectToAction("Details", "Courses", new { id = courseId }),
                _ => RedirectToAction(nameof(Index))
            };
        }

        // =============================================================
        // INDEX (Paged List of Trainees)
        // =============================================================
        public async Task<IActionResult> Index(int? page)
        {
            int pageNumber = page.GetValueOrDefault() < 1 ? 1 : page.Value;
            int pageSize = 20;

            var traineesQuery = _context.Trainees
                .AsNoTracking()
                .Include(t => t.Department)
                .OrderBy(t => t.Dept_Id)
                .ThenBy(t => t.Name);

            var pagedTrainees = await traineesQuery.ToPagedListAsync(pageNumber, pageSize);

            // TempData messages
            if (TempData["SuccessMessage"] != null)
                ViewBag.SuccessMessage = TempData["SuccessMessage"];

            if (TempData["ErrorMessage"] != null)
                ViewBag.ErrorMessage = TempData["ErrorMessage"];

            return View(pagedTrainees);
        }

        // =============================================================
        // DETAILS (With Course Results + SmartBack context)
        // =============================================================
        public async Task<IActionResult> Details(int id, string? returnTo, int? deptId, int? courseId)
        {
            var trainee = await _context.Trainees
                .Include(t => t.Department)
                .Include(t => t.CrsResults)
                    .ThenInclude(r => r.Course)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (trainee == null)
                return NotFound();

            var vm = new TraineeDetailsVM
            {
                Id = trainee.Id,
                Name = trainee.Name,
                Address = trainee.Address,
                Image = trainee.Imag,
                DepartmentName = trainee.Department?.Name ?? "No Department",
                Courses = trainee.CrsResults.Select(r => new TraineeCourseGradeVM
                {
                    CourseName = r.Course.Name,
                    TraineeDegree = r.Degree,
                    CourseDegree = r.Course.Degree,
                    MinDegree = r.Course.MinDegree
                }).ToList()
            };

            ViewBag.ReturnTo = returnTo;
            ViewBag.DepartmentId = deptId;
            ViewBag.CourseId = courseId;

            return View(vm);
        }

        // =============================================================
        // ADD (GET)
        // Can be called from:
        // - Global Trainees list      -> returnTo = "Trainees"
        // - Department Details        -> returnTo = "Department", deptId
        // - Course Details            -> returnTo = "Course", courseId
        // =============================================================
        [HttpGet]
        public async Task<IActionResult> Add(int? deptId, int? courseId, string returnTo = "Trainees")
        {
            var vm = new TraineeFormVM
            {
                Dept_Id = deptId ?? 0,
                Departments = await GetDepartmentsListAsync()
            };

            ViewBag.ReturnTo = returnTo;
            ViewBag.DepartmentId = deptId;
            ViewBag.CourseId = courseId;

            return View(vm);
        }

        // =============================================================
        // ADD (POST)
        // =============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(
            TraineeFormVM vm,
            string returnTo,
            int? deptId,
            int? courseId)
        {
            if (!ModelState.IsValid)
            {
                vm.Departments = await GetDepartmentsListAsync();

                ViewBag.ReturnTo = returnTo;
                ViewBag.DepartmentId = deptId;
                ViewBag.CourseId = courseId;

                return View(vm);
            }

            string imageName = await SaveImageAsync(vm.UploadImage);

            var trainee = new Trainee
            {
                Name = vm.Name,
                Address = vm.Address,
                Dept_Id = vm.Dept_Id,
                Imag = imageName
            };

            _context.Trainees.Add(trainee);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Trainee added successfully!";

            return SmartReturn(returnTo, deptId, courseId);
        }

        // =============================================================
        // EDIT (GET)
        // =============================================================
        [HttpGet]
        public async Task<IActionResult> Edit(int id, string? returnTo, int? deptId, int? courseId)
        {
            var trainee = await _context.Trainees.FindAsync(id);
            if (trainee == null)
                return NotFound();

            var vm = new TraineeFormVM
            {
                Id = trainee.Id,
                Name = trainee.Name,
                Address = trainee.Address,
                Dept_Id = trainee.Dept_Id,
                ExistingImage = trainee.Imag,
                Departments = await GetDepartmentsListAsync()
            };

            ViewBag.ReturnTo = returnTo;
            ViewBag.DepartmentId = deptId;
            ViewBag.CourseId = courseId;

            return View(vm);
        }

        // =============================================================
        // EDIT (POST)
        // =============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            TraineeFormVM vm,
            string returnTo,
            int? deptId,
            int? courseId)
        {
            if (!ModelState.IsValid)
            {
                vm.Departments = await GetDepartmentsListAsync();

                ViewBag.ReturnTo = returnTo;
                ViewBag.DepartmentId = deptId;
                ViewBag.CourseId = courseId;

                return View(vm);
            }

            var traineeInDb = await _context.Trainees.FindAsync(vm.Id);
            if (traineeInDb == null)
                return NotFound();

            traineeInDb.Name = vm.Name;
            traineeInDb.Address = vm.Address;
            traineeInDb.Dept_Id = vm.Dept_Id;

            // Handle new image upload
            if (vm.UploadImage != null)
            {
                string newImageName = await SaveImageAsync(vm.UploadImage);

                // Delete old image (if not default)
                if (!string.IsNullOrEmpty(traineeInDb.Imag) && traineeInDb.Imag != "default.png")
                {
                    DeleteImage(traineeInDb.Imag);
                }

                traineeInDb.Imag = newImageName;
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Trainee updated successfully!";

            return SmartReturn(returnTo, deptId, courseId);
        }

        // =============================================================
        // HELPERS
        // =============================================================

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

        private async Task<string> SaveImageAsync(IFormFile? file)
        {
            if (file == null)
                return "default.png";

            string ext = Path.GetExtension(file.FileName).ToLower();
            string[] allowed = { ".jpg", ".jpeg", ".png", ".gif" };

            if (!allowed.Contains(ext))
                return "default.png";

            string uploadsFolder = Path.Combine(_env.WebRootPath, "images");
            Directory.CreateDirectory(uploadsFolder);

            string fileName = Guid.NewGuid() + ext;
            string path = Path.Combine(uploadsFolder, fileName);

            using var fs = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(fs);

            return fileName;
        }

        private void DeleteImage(string imageName)
        {
            if (string.IsNullOrWhiteSpace(imageName))
                return;

            string path = Path.Combine(_env.WebRootPath, "images", imageName);

            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
    }
}
