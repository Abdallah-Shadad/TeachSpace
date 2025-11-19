using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TeachSpace.Models;
using TeachSpace.View_Models;
using X.PagedList; // <-- 1. ADDED THIS

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

        // INDEX -
        public async Task<IActionResult> Index(int? page)
        {
            int pageSize = 20;
            int pageNumber = (page ?? 1);
            try
            {
                var traineesQuery = _context.Trainees
                    .Include(t => t.Department)
                    .AsNoTracking()
                    .OrderBy(t => t.Dept_Id)
                    .ThenBy(t => t.Name);

                var pagedTrainees = await traineesQuery.ToPagedListAsync(pageNumber, pageSize);

                return View(pagedTrainees);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Error loading trainees: " + ex.Message;
                var emptyPagedList = new List<Trainee>().ToPagedList(pageNumber, pageSize);
                return View(emptyPagedList);
            }
        }
        // ------------------- View Trainee Detail -------------------
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var trainee = await _context.Trainees
                .AsNoTracking()
                .Include(t => t.Department)
                .Include(t => t.CrsResults)      // 1. Get the Results
                    .ThenInclude(r => r.Course)  // 2. Get the Course info for each result
                .FirstOrDefaultAsync(m => m.Id == id);

            if (trainee == null) return NotFound();

            // Map Entity to ViewModel
            var vm = new TraineeDetailsVM
            {
                Id = trainee.Id,
                Name = trainee.Name,
                Address = trainee.Address,
                Image = trainee.Imag,
                DepartmentName = trainee.Department?.Name ?? "No Department",

                // Transform the results into simple list
                Courses = trainee.CrsResults.Select(r => new TraineeCourseGradeVM
                {
                    CourseName = r.Course.Name,
                    TraineeDegree = r.Degree,
                    CourseDegree = r.Course.Degree,
                    MinDegree = r.Course.MinDegree
                }).ToList()
            };

            return View(vm);
        }

        // GET ADD
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            var vm = new TraineeFormVM
            {
                Departments = await _context.Departments
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Name
                    }).ToListAsync()
            };
            return View(vm);
        }

        // POST ADD
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(TraineeFormVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Departments = await _context.Departments
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Name
                    }).ToListAsync();

                return View(vm);
            }

            string imageName = "default.png";
            if (vm.UploadImage != null)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images");
                imageName = Guid.NewGuid() + Path.GetExtension(vm.UploadImage.FileName);
                string filePath = Path.Combine(uploadsFolder, imageName);
                using var fileStream = new FileStream(filePath, FileMode.Create);
                await vm.UploadImage.CopyToAsync(fileStream);
            }

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
            return RedirectToAction(nameof(Index));
        }

        // GET EDIT
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var trainee = await _context.Trainees.FindAsync(id);
            if (trainee == null) return NotFound();

            var vm = new TraineeFormVM
            {
                Id = trainee.Id,
                Name = trainee.Name,
                Address = trainee.Address,
                Dept_Id = trainee.Dept_Id,
                ExistingImage = trainee.Imag,
                Departments = await _context.Departments
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Name
                    }).ToListAsync()
            };

            return View(vm);
        }

        // POST EDIT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TraineeFormVM vm)
        {
            if (!ModelState.IsValid)
            {
                vm.Departments = await _context.Departments
                    .Select(d => new SelectListItem
                    {
                        Value = d.Id.ToString(),
                        Text = d.Name
                    }).ToListAsync();

                return View(vm);
            }

            var traineeInDb = await _context.Trainees.FindAsync(vm.Id);
            if (traineeInDb == null) return NotFound();

            // Update fields
            traineeInDb.Name = vm.Name;
            traineeInDb.Address = vm.Address;
            traineeInDb.Dept_Id = vm.Dept_Id;

            // Update image only if uploaded
            if (vm.UploadImage != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var ext = Path.GetExtension(vm.UploadImage.FileName).ToLower();
                if (!allowedExtensions.Contains(ext))
                {
                    ModelState.AddModelError("UploadImage", "Only image files are allowed.");
                    return View(vm);
                }

                string uploadsFolder = Path.Combine(_env.WebRootPath, "images");
                string imageName = Guid.NewGuid() + ext;
                string filePath = Path.Combine(uploadsFolder, imageName);

                using var fileStream = new FileStream(filePath, FileMode.Create);
                await vm.UploadImage.CopyToAsync(fileStream);

                // TODO: Delete the old image (traineeInDb.Imag) if it's not "default.png"

                traineeInDb.Imag = imageName;
            }


            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Trainee updated successfully!";
            return RedirectToAction(nameof(Index));
        }
    }
}